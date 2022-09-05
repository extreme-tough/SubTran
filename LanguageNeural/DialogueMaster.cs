using System;
using System.Collections.Generic;
using System.Text;

// This file contains DialogueMaster classes 


namespace DialogueMaster.Babel
{
#if _DIALOGUEMASTER
    
using System.Runtime.Serialization;
using System.ServiceModel;
    using DialogueMaster.Classification;
    using DialogueMaster.Infrastructure;
    using System.Diagnostics;
    
    [ServiceKnownType("GetKnownTypes", typeof(BabelModel))]
    [ServiceContract(Namespace = "http://dialoguemaster.de/Babel")]
    public interface IBabelService
    {
        [OperationContract]
        string ClassifyTextSimple(string text);
        [OperationContract]
        CategoryList ClassifyText(string text);
    }



        [RemotingInterface(typeof(DialogueMaster.Babel.IBabelModel))]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [KnownType(typeof(Category))]
    [KnownType(typeof(CategoryList))]
    public sealed partial class BabelModel : DialogueMaster.Infrastructure.ServiceComponent, ISerializable, IDeserializationCallback, IBabelService, DialogueMaster.Babel.IBabelModel
    {
        CategoryList IBabelService.ClassifyText(string text)
        {
            return ((IBabelModel)this).ClassifyText(text) as CategoryList;
        }

    }
    
    [DialogueMaster.Infrastructure.RemotingInterface(typeof(DialogueMaster.Babel.ITokenTable))]
    public sealed partial class TokenTable : DialogueMaster.Infrastructure.ServiceComponent, ITokenTable, ISerializable, IDeserializationCallback
    {
    
    
    #region Table Performance counters
		internal class TableCounters : IDisposable
		{
			private bool disposed = false;

			public PerformanceCounter TablesCreated=null;
			public PerformanceCounter TablesCreatedPerSecond=null;
			public PerformanceCounter TableCreationTime=null;
			public PerformanceCounter TableCreationTimeBase=null;

			public PerformanceCounter Comparisons=null;
			public PerformanceCounter ComparisonsPerSecond=null;
			public PerformanceCounter ComparisonTime=null;
			public PerformanceCounter ComparisonTimeBase=null;


			public TableCounters()
			{
				this.TablesCreated = new PerformanceCounter("DialogueMaster Babel",  "Tables created", false);
				this.TablesCreatedPerSecond = new PerformanceCounter("DialogueMaster Babel",  "Tables created / sec", false);
				this.TableCreationTime= new PerformanceCounter("DialogueMaster Babel",  "Avg. table creation time", false);
				this.TableCreationTimeBase= new PerformanceCounter("DialogueMaster Babel",  "base for avg. table creation time", false);
				this.Comparisons = new PerformanceCounter("DialogueMaster Babel",  "Comparisons", false);
				this.ComparisonsPerSecond = new PerformanceCounter("DialogueMaster Babel",  "Comparisons / sec", false);
				this.ComparisonTime = new PerformanceCounter("DialogueMaster Babel",  "Avg. comparison time",  false);
				this.ComparisonTimeBase = new PerformanceCounter("DialogueMaster Babel",  "base for Avg. comparison time", false);

				System.Diagnostics.Debug.WriteLine("Created table Performance Counter instances");
			}

			~TableCounters()
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
						this.TablesCreated.RemoveInstance();
						this.TablesCreatedPerSecond.RemoveInstance();
						this.TableCreationTime.RemoveInstance();
						this.TableCreationTimeBase.RemoveInstance();

						this.TablesCreated.Dispose();
						this.TablesCreatedPerSecond.Dispose();
						this.TableCreationTime.Dispose();
						this.TableCreationTimeBase.Dispose();


						this.Comparisons.RemoveInstance();
						this.ComparisonsPerSecond.RemoveInstance();
						this.ComparisonTime.RemoveInstance();
						this.ComparisonTimeBase.RemoveInstance();

						this.Comparisons.Dispose();
						this.ComparisonsPerSecond.Dispose();
						this.ComparisonTime.Dispose();
						this.ComparisonTimeBase.Dispose();

						
						System.Diagnostics.Debug.WriteLine("Removed table Performance Counter instances");
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

        /// <summary>
		/// Set this to false to disable performance counters
		/// </summary>
		private static bool UseCounters = true;
		private static TableCounters m_Counters = null;
    }

    [DialogueMaster.Infrastructure.RemotingInterface(typeof(DialogueMaster.Babel.ITokenStats))]
    public sealed partial class TokenStats : DialogueMaster.Infrastructure.ServiceComponent
    {
    }

#else




    public interface ITokenStats
    {
        int Occurences { get; }
        int Position { get; set; }
        int Rank { get; set; }
        string Token { get; }

        void AddOccurence();
        int CompareTo(object obj);
    }

    // In dialogue master this is defined in DialogueMaster.Core.Interfaces
    public interface ITokenTable
    {
        
        ITokenStats this[string token] {get;}
        int Count { get; }
        bool Enabled { get; set; }
        ICollection<string> Keys { get; }
        int Ranks { get; set; }
        
        
        ICollection<ITokenStats> Values { get; }
        ITokenTable WordTable { get; }
        ITokenTable CharsetTable { get; }

        void Add(string key, ITokenStats stats);
        int PositionOf(string Token);
        int RankOf(string Token);
        double ComparisonScore(ITokenTable otherTable, double cutOf);
        double WordComparisonScore(ITokenTable otherTable, double cutOf, ref int hits);
        double CharsetComparisonScore(ITokenTable otherTable, double cutOf);

    }



    public interface IBabelModel
    {
        IBabelModel DefaultModel { get; }
        ICollection<string> Keys { get; }
        IBabelModel LargeModel { get; }
        IBabelModel SmallModel { get; }
        ICollection<ITokenTable> Values { get; }

        ITokenTable this[string key] { get; }

        DialogueMaster.Classification.ICategoryList ClassifyText(string text);
    }

#endif
}

#if !_DIALOGUEMASTER
namespace     DialogueMaster.Classification
{
    #region simple implementation of a category list


    public interface ICategoryList  : IList<ICategory>
    {
        /// <summary>
        /// Add a new Category to the list
        /// </summary>
        /// <param name="categoryName">name of the catgory</param>
        /// <param name="score">the score of the category</param>
        /// <returns>the newly added <see cref="ICategory"/></returns>
        ICategory Add(string categoryName, double score);



    }

        /// <summary>
    /// Holds information about a classification result for a specific category. It is usally used in combination with an <see cref="ICategoryList"/>.
    /// </summary>
    public interface ICategory : IComparable, System.Collections.IComparer
    {
        /// <summary>
        /// Gets or sets the score that the catogry got from classification
        /// </summary>
        double Score { get; set; }
        /// <summary>
        /// Gets the Name of the Category
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Gets some additional data
        /// </summary>
        object Tag { get; set; }
    }



    [Serializable]
    public class Category : ICategory
    {
        private object m_Tag;
        private String m_Name;
        private double m_Score;

        public Category(String name, double score)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            this.m_Name = name;
            this.m_Score = score;
        }


        public override  String ToString()
        {
            return String.Format("[{0}:{1,0:F}]", m_Name, m_Score);
        }



       
        #region ICategory Members

        public object Tag
        {
            get
            {
                return this.m_Tag;
            }
            set
            {
                this.m_Tag = value;
            }
        }


        public double Score
        {
            get
            {
                return this.m_Score;
            }
            set
            {
                this.m_Score = value;
            }
        }

        public string Name
        {
            get
            {
                return this.m_Name;
            }
            set
            {
                if (this.m_Name == null)
                    throw new ArgumentNullException("value");
                this.m_Name = value;

            }
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
		{
			Category cObj = obj as Category;
			if (cObj != null)
			{
				if ( cObj.Score > m_Score  )
					return 1;
				else
					if ( cObj.Score < m_Score )
						return -1;
				return 0;
						
			}
			else 
				// try to compare the socore to what ever...
				return this.m_Score.CompareTo(cObj);
		}

		#endregion

		#region IComparer Members

		public int Compare(object x, object y)
		{
			Category cX = x as Category;
			Category cY = y as Category;
			
			if ( (cX != null) && (cY != null) )
			{
				if ( cX.Score > cY.Score  )
					return 1;
				else
					if ( cX.Score < cY.Score )
					return -1;
						
			}
			return 0;
		}

		#endregion

    }

    [Serializable]
    public class CategoryList : List<ICategory>, ICategoryList
    {

        public override String ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < this.Count; i++)
                sb.Append(this[i].ToString());
            return sb.ToString();
        }

        public Category this[string name]
        {
            get
            {
                foreach (Category cat in this)
                    if (cat.Name.Equals(name))
                        return cat;
                throw new ArgumentOutOfRangeException("name");
            }
        }


        #region ICategoryList Members

        public ICategory Add(string categoryName, double score)
        {
            ICategory result = new Category(categoryName, score);
            this.Add(result);
            return result;
        }

        #endregion
    }



    #endregion 
}
#endif