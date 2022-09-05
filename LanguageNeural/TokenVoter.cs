using System;
using System.Collections;
namespace DialogueMaster.Babel
{
	/// <summary>
	/// Collects the number of votes for a single token.
	/// </summary>
 	internal sealed class TokenVoter : IComparable		
	{

		#region Fields (2) 

		private string m_Category;
		private double m_Score = 1;

		#endregion Fields 

		#region Constructors (2) 

		public TokenVoter(string category, double score)
		{
			this.m_Category = category;
			this.m_Score = score;
		}

		public TokenVoter(string category)
		{
			this.m_Category = category;
		}

		#endregion Constructors 

		#region Properties (2) 

		public string Category
		{
			get {return this.m_Category;}
		}

		public double Score
		{
			get {return this.m_Score;}
			set {this.m_Score = value;}
		}

		#endregion Properties 

		#region Methods (2) 


		// Public Methods (2) 

		public void AddOccurence()
		{
			this.m_Score++;
		}

		public override string ToString()
		{
			return this.m_Category+":"+this.m_Score.ToString();
		}


		#endregion Methods 


		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is TokenVoter)
			{
				return -1* this.m_Score.CompareTo( ((TokenVoter)obj).Score);
			}
			return 0;
		}

		#endregion
	}

}
