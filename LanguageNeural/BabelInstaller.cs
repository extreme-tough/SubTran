using System;
using System.Collections;
using System.Diagnostics;
using System.Configuration.Install;
using System.Management.Instrumentation;

namespace DialogueMaster.Babel
{
	/// <summary>
	/// Installs the counters for Babel.
	/// </summary>
	public class Installer : System.Management.Instrumentation.DefaultManagementProjectInstaller
	{
		public Installer()
		{
		}
		
		public static void InstallCounters()
		{
            if (!PerformanceCounterCategory.Exists("DialogueMaster Babel")) 
			{

				CounterCreationDataCollection ccdJanusTables = new CounterCreationDataCollection();

				// tables
				CounterCreationData cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.NumberOfItems64;
				cd.CounterName = "Tables created";
				cd.CounterHelp = "Total number of NGram-Tables created";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
				cd.CounterName = "Tables created / sec";
				cd.CounterHelp = "Number of NGram-Tables created per second";
				ccdJanusTables.Add(cd);


				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawFraction;
				cd.CounterName = "Avg. table creation time";
				cd.CounterHelp = "Average time it takes to create a table";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawBase;
				cd.CounterName = "base for avg. table creation time";
				cd.CounterHelp = "base for Average time it takes to create a table";
				ccdJanusTables.Add(cd);


				// Comparison
				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.NumberOfItems64;
				cd.CounterName = "Comparisons";
				cd.CounterHelp = "Total number of comparisons";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
				cd.CounterName = "Comparisons / sec";
				cd.CounterHelp = "Number of comparisons created per second";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawFraction;
				cd.CounterName = "Avg. comparison time";
				cd.CounterHelp = "Average time it takes to compare two tables";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawBase;
				cd.CounterName = "base for Avg. comparison time";
				cd.CounterHelp = "base for Average time it takes to compare two tables";
				ccdJanusTables.Add(cd);

				// Classifications
				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.NumberOfItems64;
				cd.CounterName = "Classifications";
				cd.CounterHelp = "Total number of classifications";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
				cd.CounterName = "Classifications / sec";
				cd.CounterHelp = "Number of classifications created per second";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawFraction;
				cd.CounterName = "Avg. classification time";
				cd.CounterHelp = "Average time it takes to classify a text";
				ccdJanusTables.Add(cd);

				cd = new CounterCreationData();
				cd.CounterType = PerformanceCounterType.RawBase;
				cd.CounterName = "base for Avg. classification time";
				cd.CounterHelp = "base for Average time it takes to classify a text";
				ccdJanusTables.Add(cd);

                PerformanceCounterCategory.Create("DialogueMaster Babel", "DialogueMaster Janus language detection engine", PerformanceCounterCategoryType.SingleInstance, ccdJanusTables);
			}
		}

		public static void UninstallCounters()
		{
            if (PerformanceCounterCategory.Exists("DialogueMaster Babel")) 
			{
                PerformanceCounterCategory.Delete("DialogueMaster Babel");
			}
		}

		public override void Install(IDictionary stateSaver)
		{
			base.Install(stateSaver);
			InstallCounters();
			
		}

		
	}
}
