using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Resources;
using System.Reflection;
using DialogueMaster.Classification;
using System.Collections.Generic;

#if _DIALOGUEMASTER
using System.ServiceModel;
using DialogueMaster.Infrastructure;
#endif

namespace DialogueMaster.Babel
{
	/// <summary>
	/// Babel's main class.
    /// Use the provided Models to classify a text or assemble your own model
	/// </summary>
    [Serializable()]
    public sealed partial  class BabelModel : ISerializable, IDeserializationCallback, IBabelModel
{

		#region Fields (5) 

		[NonSerialized()]
		private static ModelCounters m_Counters = null;
        private int m_MaxVoters = 10;
		/// <summary>
		///  Reported matches are those fingerprints with a score less than best
		///  score * THRESHOLDVALUE (i.e. a THRESHOLDVALUE of 0.03 means matches
		///  must score within 3% from the best score.)  
		/// </summary>
		private double m_Threshold = 0.55f;
        public const int MAX_TOKENS_DEFAULT = 400;
		/// <summary>
		/// Set this to false to disable performance counters
		/// </summary>
		private static bool UseCounters = false;

		#endregion Fields 

		#region Properties (1) 

		public ITokenTable this[string lang]
		{
			get
			{
				return this.m_InnerDictionary[lang] as ITokenTable;
			}
		}

		#endregion Properties 

		#region Methods (5) 


		// Public Methods (5) 

        public void AddFile(string category, string fileName)
        {

        }

        public void AddFile(string category, string fileName, List<char> charsetFilter)
		{
            TokenTable newTable = new TokenTable(MAX_TOKENS_DEFAULT);
            newTable.CreateFromFile(fileName, charsetFilter);
			this.Add(category, newTable);
		}

        public ICategoryList ClassifyText(string text)
        {
            return ClassifyText(text, 2);
        }

        public ICategoryList ClassifyText(string text, int maxResults)
		{
			long startTime = DateTime.Now.Ticks;
			ListDictionary results = new ListDictionary();
            Dictionary<string, double> scores = new Dictionary<string, double>();
			TokenTable tblTest = new TokenTable(text);
            double maxScore = 0;
            double threshold = 0;


            List<TokenVoter> charsetVoters = new List<TokenVoter>();
            // collect stats based on charset (first filter)
            foreach(string category in this.Keys)
			{
				ITokenTable catTable = this[category];
				if (!catTable.Enabled)
					continue;
                double score = catTable.CharsetTable.CharsetComparisonScore(tblTest.CharsetTable, threshold);

				if (score > maxScore)
				{
					maxScore = score;
					threshold = (maxScore* this.m_Threshold); 
				}
                if (score > threshold)
                    charsetVoters.Add(new TokenVoter(category, score));
			}

            // chinese does not have a "Charset"... so to be sure....
            if ( (charsetVoters.Count < 3) && (this.Keys.Contains("zh")) )
                charsetVoters.Add(new TokenVoter("zh") );

            charsetVoters.Sort();
            for (int i = charsetVoters.Count - 1; i > -1; i--)
            {
                if (charsetVoters[i].Score < threshold)
                    charsetVoters.RemoveAt(i);
            }


            maxScore = 0; ;
			// collect scores for each table
			int maxWordHits=0;
            threshold = 0;
            foreach (TokenVoter charVoter in charsetVoters)
			{
                ITokenTable catTable = this[charVoter.Category];
				if (!catTable.Enabled)
					continue;
				int hits = 0;
				double score =  catTable.WordComparisonScore(tblTest, threshold, ref hits);
				if (hits > maxWordHits)
					maxWordHits=hits;

				if (score > maxScore)
				{
					maxScore = score;
					threshold = (maxScore* this.m_Threshold); 
				}
                if (score > threshold)
                    scores.Add(charVoter.Category, score);
			}

            double sumScore = 0;
            List<TokenVoter> voters = new List<TokenVoter>();
            if (scores.Count == 0)
            {
                maxScore = charsetVoters[0].Score; ;
                // take the voters from the closed charsert
                foreach (TokenVoter v in charsetVoters)
                {

                    scores.Add(v.Category, v.Score);
                }

            }
            threshold = (maxScore * m_Threshold);


            // copy the scores to a sorted voters list
            foreach (string key in scores.Keys)
            {
                /*	if ((long)scores[key] < threshold)
                        continue; 
                        */
                // calc sum score
                double score = scores[key];
                /*
                if (maxWordHits < 1)
                {
                    score = 0; //  score > 0 ? 1 : 0;
                }
                else*/
                if (score > threshold)
                {
                    score /= maxScore;
                    if (maxWordHits > 0)
                        score /= maxWordHits;
                    score *= 100;
                    sumScore += score;
                    voters.Add(new TokenVoter(key, score));
                }

               
            }


            if (voters.Count > 1)
            {
                if (sumScore > 0)
                {
                    voters.Sort();
                    // cleanup voters and rebalance if more than 3 voters...
                    if (voters.Count > m_MaxVoters)
                    {
                        sumScore = 0;
                        for (int i = 0; i < m_MaxVoters; i++)
                        {
                            ((TokenVoter)voters[i]).Score -= ((TokenVoter)voters[m_MaxVoters]).Score;
                            sumScore += ((TokenVoter)voters[i]).Score;
                        }
                        voters.RemoveRange(m_MaxVoters, voters.Count - m_MaxVoters);
                    }
                }
            }

			// now normalize results..
			// the results are not an absolute confidence
			// but relative 
			if (voters.Count == 1)
			{
				// only one voter, so it we are 100% sure that's the one!
                ScoreHolder newScore = new ScoreHolder(100);
                results.Add(((TokenVoter)voters[0]).Category, newScore);
			}
			else
			{
				for(int i=0;i<voters.Count;i++)
				{
					TokenVoter stats = voters[i] as TokenVoter;
					
					double percScore =    sumScore > 0 ?   (stats.Score/ sumScore)*100 : 0;
                    results.Add(stats.Category,new ScoreHolder(percScore));
				}


				// if we have more than one possible result
				// we will try to disambiguate it by checking for 
				// very common words 
				if ( (results.Count == 0) || (results.Count > 1) )
				{
					scores.Clear();
					maxScore = 0;
					threshold = 0;
					// collect scores for each table
					foreach(string category in results.Keys)
					{
						ITokenTable catTable = (ITokenTable) this[category];
						// threshold = tblTest.WordTable. Ranks*catTable.WordTable.Count;
                        double score = catTable.ComparisonScore(tblTest, threshold);
						if (score>0)
						{
							maxScore = System.Math.Max(maxScore,score);
							scores.Add(category,score);
						}
					}
					// got results?
					if (scores.Count > 0)
					{

						sumScore = 0;
						// copy the scores to a sorted voters list
						voters.Clear();
						foreach(string key in scores.Keys)
						{
							// calc sum score
							sumScore +=  scores[key];
							voters.Add(new TokenVoter(key,scores[key]));
						}
						voters.Sort();
				

						// now normalize results..
						// the results are not an absolute confidence
						// but relative 
						if (voters.Count == 1)
						{
							// only one voter, so all other results are only 3/4 value
							foreach(string category in results.Keys)
								if (category != ((TokenVoter)voters[0]).Category)
									((ScoreHolder)results[category]).DevideScore(0.75);
						}
						else
						{
							for(int i=0;i<voters.Count;i++)
							{
								TokenVoter stats = voters[i] as TokenVoter;
						
								double percScore = (stats.Score/ sumScore)*200;
								((ScoreHolder)results[stats.Category]).AddScore(percScore);
							}
							foreach(string category in results.Keys)
								((ScoreHolder)results[category]).DevideScore(0.75);

						}
					}

				}
			}
			// now build a proper result..
			voters.Clear();
			foreach(string key in results.Keys)
			{
                voters.Add(new TokenVoter(key,((ScoreHolder)results[key]).Score));
			}
			voters.Sort();

            /*
            // Do a distance to next boos 
            for (int i = 0; i < voters.Count-1; i++)
            {
                voters[i].Score += voters[i].Score - voters[i + 1].Score;
            }
            */

            // reduce to maximum results
            if (voters.Count > maxResults)
			{
                voters.RemoveRange(maxResults, voters.Count - maxResults);
			}

            
            // re-weight...
			double dSumScore = 0;
			foreach(TokenVoter voter in voters)
			{
				dSumScore+=voter.Score;
			}
			results.Clear();
			foreach(TokenVoter voter in voters)
			{
				results.Add(voter.Category, new ScoreHolder((voter.Score / dSumScore)*100));
			}
//			ArrayList resultList = new ArrayList(results.Values);
//			resultList.Sort
            CategoryList result = new CategoryList();
			foreach(string category in results.Keys)
                result.Add(new Category(category, ((ScoreHolder)results[category]).Score) );
            result.Sort();
#if DIALOGUEMASTER
			if (UseCounters)
			{
				m_Counters.Classifications.Increment();
				m_Counters.ClassificationsPerSecond.Increment();
				m_Counters.ComparisonTime.IncrementBy(DateTime.Now.Ticks - startTime);
				m_Counters.ComparisonTimeBase.Increment();
			}
#endif
			tblTest.Clear();
			return result;
		}

        public void Remove(String lang)
        {
            if (this.m_InnerDictionary.ContainsKey(lang))
                this.m_InnerDictionary.Remove(lang);
        }


		#endregion Methods 
#if _DIALOGUEMASTER
        private readonly RemotableValuesDictionary<string, ITokenTable> m_InnerDictionary = new RemotableValuesDictionary<string, ITokenTable>();
#else
        private readonly Dictionary<string, ITokenTable> m_InnerDictionary = new Dictionary<string, ITokenTable>();
#endif


		#region Model Performance counters
		internal class ModelCounters : IDisposable
		{
			private bool disposed;

			public PerformanceCounter Classifications;
			public PerformanceCounter ClassificationsPerSecond;
			public PerformanceCounter ComparisonTime;
			public PerformanceCounter ComparisonTimeBase;


			public ModelCounters()
			{
				this.Classifications = new PerformanceCounter("25hours Babel",  "Classifications", "", false);
				this.ClassificationsPerSecond = new PerformanceCounter("25hours Babel",  "Classifications / sec", "", false);
				this.ComparisonTime = new PerformanceCounter("25hours Babel",  "Avg. classification time", "", false);
				this.ComparisonTimeBase = new PerformanceCounter("25hours Babel",  "base for Avg. classification time", "", false);

				System.Diagnostics.Debug.WriteLine("Created model Performance Counter instances");
			}

			~ModelCounters()
			{
				Dispose(false);
			}


			#region IDisposable Members

			private void Dispose(bool disposing)
			{
				// Check to see if Dispose has already been called.
				if(!this.disposed)
				{
					// If disposing equals true, dispose all managed 
					// and unmanaged resources.
					if(disposing)
					{
						// Dispose managed resources.

						this.Classifications.RemoveInstance();
						this.ClassificationsPerSecond.RemoveInstance();
						this.ComparisonTime.RemoveInstance();
						this.ComparisonTimeBase.RemoveInstance();

						this.Classifications.Dispose();
						this.ClassificationsPerSecond.Dispose();
						this.ComparisonTime.Dispose();
						this.ComparisonTimeBase.Dispose();

						
						System.Diagnostics.Debug.WriteLine("Removed model Performance Counter instances");
					}
				}
				disposed = true;         
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion
		}
		#endregion

		#region ScoreByRefHolder
		internal sealed class ScoreHolder
		{
			private double m_Score;
			public ScoreHolder(double initialScore)
			{
				this.m_Score = initialScore;
			}

			public void AddScore(double changeBy)
			{
				this.m_Score+=changeBy;
			}

			public void DevideScore(double devideBy)
			{
                this.m_Score/=devideBy;
                
			}

			public double Score
			{
				get
				{
					return this.m_Score;
				}
			}

			public override string ToString()
			{
				return this.m_Score.ToString();
			}


		}
		#endregion

		#region constructors
		public BabelModel()
		{
			if (UseCounters && (m_Counters == null) )
				m_Counters = new ModelCounters();
			
		}


		#endregion

		#region Serialization

        static BabelModel s_DefaultModel;
        static BabelModel s_SmallModel;
        static BabelModel s_LargeModel;
        static BabelModel s_AllModel;
        static BabelModel s_CommonModel;
        static object s_LoaderLock = new object();

        /// <summary>
        /// The AllModel contains all available languages
        /// </summary>
        /// <remarks>
        /// ar	Arabic
        /// bn	Bengali (Bangladesh)
        /// ca	Catalan
        /// cs	Czech
        /// da	Danish
        /// de	German
        /// el	Greek
        /// en	English
        /// es	Spanish
        /// et	Estonian
        /// fa	Persian
        /// fi	Finnish
        /// fr	French
        /// he	Hebrew
        /// hi	Hindi
        /// hu	Hungarian
        /// is	Icelandic
        /// it	Italian
        /// ja	Japanese
        /// ko	Korean
        /// nl	Dutch
        /// nn	Norwegian, Nynorsk (Norway)
        /// no	Norwegian
        /// pl	Polish
        /// pt	Portuguese
        /// ro	Romanian
        /// ru	Russian
        /// se	Sami (Northern) (Sweden)
        /// sl	Slovenian
        /// sr	Serbian
        /// sv	Swedish
        /// th	Thai
        /// tr	Turkish
        /// vi	Vietnamese
        /// zh	Chinese (Simplified)
        /// </remarks>
        public IBabelModel AllModel
        {
            get { return _AllModel; }
        }

        /// <summary>
        /// The DefaultModel contains the languages detectable by DialogueMaster by default
        /// </summary>
        /// <remarks>
        /// da	Danish
        /// de	German
        /// en	English
        /// es	Spanish
        /// fr	French
        /// nl	Dutch
        /// no	Norwegian
        /// pt	Portuguese
        /// sv	Swedish
        /// </remarks>
        public IBabelModel DefaultModel
        {
            get { return _DefaultModel; }
        }


        /// <summary>
        /// The SmallModel contains only the most common languages needed by DialogueMasgter
        /// </summary>
        /// <remarks>
        /// de	German
        /// en	English
        /// es	Spanish
        /// fr	French
        /// it	Italian
        /// </remarks>
        public IBabelModel SmallModel
        {
            get { return _SmallModel; }
        }

        /// <summary>
        /// The LargeModel contains the extended set of detectable languages
        /// </summary>
        /// <remarks>
        /// cs	Czech
        /// da	Danish
        /// de	German
        /// el	Greek
        /// en	English
        /// es	Spanish
        /// fi	Finnish
        /// fr	French
        /// hu	Hungarian
        /// it	Italian
        /// nl	Dutch
        /// no	Norwegian
        /// pl	Polish
        /// pt	Portuguese
        /// ru	Russian
        /// sv	Swedish
        /// tr	Turkish
        /// </remarks>
        public IBabelModel LargeModel
        {
            get { return _LargeModel; }
        }

        /// <summary>
        /// The CommonModel contains the 10 most spread languages in the world
        /// </summary>
        /// <remarks>
        /// ar	Arabic
        /// bn	Bengali (Bangladesh)
        /// de	German
        /// en	English
        /// es	Spanish
        /// fr	French
        /// hi	Hindi
        /// ja	Japanese
        /// pt	Portuguese
        /// ru	Russian
        /// zh	Chinese (Simplified)
        /// </remarks>
        public IBabelModel CommonModel
        {
            get { return _CommonModel; }
        }

        /// <summary>
        /// Static version of the _DefaultModel
        /// </summary>
        /// <see cref="_DefaultModel"/>
        public static BabelModel _DefaultModel
		{
			get
			{
                lock (s_LoaderLock)
                {
                    if (s_DefaultModel == null)
                    {
                        s_DefaultModel = new BabelModel();
                        s_DefaultModel.Add("en", _AllModel["en"]);
                        s_DefaultModel.Add("de", _AllModel["de"]);
                        s_DefaultModel.Add("fr", _AllModel["fr"]);
                        s_DefaultModel.Add("es", _AllModel["es"]);
                        s_DefaultModel.Add("it", _AllModel["it"]);
                        
                        s_DefaultModel.Add("nl", _AllModel["it"]);
                        s_DefaultModel.Add("sv", _AllModel["it"]);
                        s_DefaultModel.Add("da", _AllModel["it"]);
                        s_DefaultModel.Add("no", _AllModel["it"]);
                        s_DefaultModel.Add("pt", _AllModel["it"]);


                    }
                    return s_DefaultModel;
                }
			}
		}


        /// <summary>
        /// Static version of the CommonModel
        /// </summary>
        /// <see cref="CommonModel"/>
        public static BabelModel _CommonModel
        {
            get
            {
                lock (s_LoaderLock)
                {
                    if (s_CommonModel == null)
                    {
                        s_CommonModel = new BabelModel();
                        s_CommonModel.Add("zh", _AllModel["zh"]);
                        s_CommonModel.Add("es", _AllModel["es"]);
                        s_CommonModel.Add("en", _AllModel["en"]);
                        s_CommonModel.Add("bn", _AllModel["bn"]);
                        s_CommonModel.Add("hi", _AllModel["hi"]);
                        s_CommonModel.Add("ar", _AllModel["ar"]);
                        s_CommonModel.Add("pt", _AllModel["pt"]);
                        s_CommonModel.Add("ru", _AllModel["ru"]);
                        s_CommonModel.Add("ja", _AllModel["ja"]);
                        s_CommonModel.Add("de", _AllModel["de"]);
                        s_CommonModel.Add("fr", _AllModel["fr"]);

                    }
                    return s_CommonModel;
                }
            }
        }


        /// <summary>
        /// Static version of the SmallModel
        /// </summary>
        /// <see cref="SmallModel"/>
        public static BabelModel _SmallModel
		{
			get
            {
                lock (s_LoaderLock)
                {
                    if (s_SmallModel == null)
                    {
                        s_SmallModel = new BabelModel();
                        s_SmallModel.Add("en", _AllModel["en"]);
                        s_SmallModel.Add("de", _AllModel["de"]);
                        s_SmallModel.Add("fr", _AllModel["fr"]);
                        s_SmallModel.Add("es", _AllModel["es"]);
                        s_SmallModel.Add("it", _AllModel["it"]);
                    }
                    return s_SmallModel;
                }
			}
		}

        /// <summary>
        /// Static version of the LargeModel
        /// </summary>
        /// <see cref="LargeModel"/>
        public static BabelModel _LargeModel
		{
			get
            {
                lock (s_LoaderLock)
                {
                    if (s_LargeModel == null)
                    {
                        s_LargeModel = new BabelModel();
                        s_LargeModel.Add("en", _AllModel["en"]);
                        s_LargeModel.Add("de", _AllModel["de"]);
                        s_LargeModel.Add("fr", _AllModel["fr"]);
                        s_LargeModel.Add("es", _AllModel["es"]);
                        s_LargeModel.Add("it", _AllModel["it"]);

                        s_LargeModel.Add("nl", _AllModel["nl"]);
                        s_LargeModel.Add("sv", _AllModel["sv"]);
                        s_LargeModel.Add("da", _AllModel["da"]);
                        s_LargeModel.Add("no", _AllModel["no"]);
                        s_LargeModel.Add("pt", _AllModel["pt"]);

                        s_LargeModel.Add("ru", _AllModel["ru"]);
                         s_LargeModel.Add("el", _AllModel["el"]);
                        s_LargeModel.Add("tr", _AllModel["tr"]);
                        s_LargeModel.Add("cs", _AllModel["cs"]);
                        s_LargeModel.Add("pl", _AllModel["pl"]);

                        s_LargeModel.Add("hu", _AllModel["hu"]);
//                        s_LargeModel.Add("is", _AllModel["is"]);
                        s_LargeModel.Add("fi", _AllModel["fi"]);


                    }
                    return s_LargeModel;
                }
			}
		}


        /// <summary>
        /// Static version of the AllModel
        /// </summary>
        /// <see cref="AllModel"/>
        public static BabelModel _AllModel
		{
			get
			{
                lock (s_LoaderLock)
                {
                    if (s_AllModel == null)
                    {
                        Assembly a = Assembly.GetExecutingAssembly();
                        using (Stream s = a.GetManifestResourceStream("DialogueMaster.Babel.models.all.model"))
                        {
                            s_AllModel = LoadFromStream(s);
                        }
                    }
                    return s_AllModel;
                }
			}
		}

        /// <summary>
        /// Saves the model to a file
        /// </summary>
        /// <param name="fileName">the target file name</param>
		public void SaveToFile(string fileName)
		{
			using (FileStream fs = new FileStream(fileName, FileMode.Create))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(fs,this);
				fs.Close();
			}
		}

        /// <summary>
        /// Loads a model from a stream
        /// </summary>
        /// <param name="stream">the stream in which the model is stored</param>
        /// <returns>the loaded BabelModel</returns>
		public static BabelModel LoadFromStream(Stream stream)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			return formatter.Deserialize(stream) as BabelModel;
		}

        /// <summary>
        /// Loads a model from a given file
        /// </summary>
        /// <param name="fileName">the name of the file in which the model is stored</param>
        /// <returns>the loaded BabelModel</returns>
        public static BabelModel LoadFromFile(string fileName)
		{
			BabelModel newModel;
			using (FileStream fs = new FileStream(fileName, FileMode.Open))
			{
				newModel = LoadFromStream(fs);
				fs.Close();
			}
			return newModel;
		}



		#endregion

		#region ISerializable Members
        [NonSerialized()]
		SerializationInfo m_siInfo=null;
		private BabelModel(SerializationInfo info,StreamingContext context)
		{
			if (UseCounters && (m_Counters == null) )
				m_Counters = new ModelCounters();

			this.m_siInfo = info;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			String[] objArray1 = new string[this.Count];
			TokenTable[] objArray2 = new TokenTable[this.Count];
			int i=0;
			foreach(String key in this.Keys)
			{
				objArray1[i]=key;
				i++;
			}
			i=0;
			foreach(TokenTable val in this.Values)
			{
				objArray2[i]=val;
				i++;
			}
			info.AddValue("Keys", objArray1, typeof(String[]));
			info.AddValue("Values", objArray2, typeof(TokenTable[]));
		}

		#endregion

		#region IDeserializationCallback Members

		public void OnDeserialization(object sender)
		{
			if (this.m_siInfo == null)
			{
				throw new SerializationException("Something went wrong during deserialization");
			}

			string[] objArray1 = (string[])this.m_siInfo.GetValue("Keys",typeof(string[]));
			TokenTable[] objArray2 = (TokenTable[])this.m_siInfo.GetValue("Values",typeof(TokenTable[]));
			for(int i=0;i<objArray1.Length;i++)
				this.Add(objArray1[i],objArray2[i]);

		}

		#endregion

        #region IDictionary Members

        public void Add(object key, object value)
        {
            this.m_InnerDictionary.Add(key.ToString(), value as ITokenTable);
        }

        
      

        public ICollection<string> Keys
        {
            get { return this.m_InnerDictionary.Keys; }
        }

        public ICollection<ITokenTable> Values
        {
            get { return this.m_InnerDictionary.Values; }
        }

        #endregion

        #region ICollection Members

        public int Count
        {
            get { return this.m_InnerDictionary.Count; }
        }

        #endregion

        #region IBabelService Members

        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            System.Collections.Generic.List<System.Type> knownTypes =
                new System.Collections.Generic.List<System.Type>();
            // Add any types to include here.
            knownTypes.Add(typeof(Category));
            return knownTypes;
        }


        public string ClassifyTextSimple(string text)
        {
            return ClassifyText(text).ToString();
        }

       
        #endregion
    }
}

    