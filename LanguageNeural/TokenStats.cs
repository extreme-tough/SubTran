using System;
using System.Collections;
using System.Runtime.Serialization;
namespace DialogueMaster.Babel
{
	/// <summary>
	/// Holds the statistic for a single Token
	/// </summary>
	[Serializable]
    public sealed partial class TokenStats : IComparable, ISerializable, ITokenStats
	{

		#region Fields (4) 

		private string m_Token;
		private int m_Occurences = 1;
		private int m_Position = 0;
		private int m_Rank = 0;

		#endregion Fields 

		#region Constructors (2) 

        /// <summary>
        /// Creates a new TokenStats for the given NGram with the initial number of occurences
        /// </summary>
        /// <param name="token">the token</param>
        /// <param name="occurences">number of initial occurences</param>
		public TokenStats(string token, int occurences)
		{
			this.m_Token = token;
			this.m_Occurences = occurences;
		}

        /// <summary>
        /// Creates a new TokenStats for the given NGram with initially one   occurence
        /// </summary>
        /// <param name="token">the token</param>
        public TokenStats(string token)
            : this(token, 1)
		{
		}

		#endregion Constructors 

		#region Properties (4) 

        /// <summary>
        /// Number of occuences (within the test data)
        /// </summary>
		public int Occurences
		{
			get {return this.m_Occurences;}
            set {this.m_Occurences = value;}
		}

        /// <summary>
        /// Postion in the statistics table (unique among all TokenStats in one Table)
        /// </summary>
		public int Position
		{
			get
			{
				return this.m_Position;
			}
			set
			{
				this.m_Position = value;
			}
		}

        /// <summary>
        /// Rank within the Table (multiple tokens with identical occurences share one rank)
        /// </summary>
		public int Rank
		{
			get
			{
				return this.m_Rank;
			}
			set
			{
				this.m_Rank = value;
			}
		}

        /// <summary>
        /// the token
        /// </summary>
		public string Token
		{
			get {return this.m_Token;}
		}

		#endregion Properties 

		#region Methods (3) 


		// Public Methods (3) 
        /// <summary>
        /// Increases the occurences by one
        /// </summary>
		public void AddOccurence()
		{
			this.m_Occurences++;
		}

        /// <summary>
        /// The hash code for a TokenStats is the HashCode of its token
        /// </summary>
        /// <returns></returns>
		public override int GetHashCode()
		{
			return this.m_Token.GetHashCode();
		}

		public override string ToString()
		{
			return this.m_Token+":"+this.m_Rank.ToString()+":"+this.m_Occurences.ToString();
		}


		#endregion Methods 
		
        
        // compares by the score, not the NGRam name
		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is TokenStats)
			{

				int result = -1 * this.m_Occurences.CompareTo( ((TokenStats)obj).Occurences);
				// same number of occurences?
				if (result == 0) 
				{
					// sort by length
					result = this.m_Token.Length.CompareTo(((TokenStats)obj).Token.Length);
					// same length=
					if (result == 0)
					{
						// sort by alpha
						result = this.m_Token.CompareTo(((TokenStats)obj).Token);
					}
				}
				return result;
			}
			return 0;
		}

		#endregion

		#region ISerializable Members
		private TokenStats(SerializationInfo info, StreamingContext context)
		{
			this.m_Token = info.GetString("Token");
			this.m_Occurences = info.GetInt32("Occurences");
			this.m_Rank = info.GetInt32("Rank");
			this.m_Position = info.GetInt32("Position");
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Token",this.m_Token);
			info.AddValue("Occurences",this.m_Occurences);
			info.AddValue("Rank",this.m_Rank);
			info.AddValue("Position",this.m_Position);
		}

		#endregion
	}

}

