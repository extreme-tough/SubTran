#if DIALOGUEMASTER

using System;

namespace DialogueMaster.Babel
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
	//		Installer.UninstallCounters();
			Installer.InstallCounters();

			DateTime start = DateTime.Now;
			System.Console.Out.WriteLine("Reading model");
			// CREATE NEW MODEL FROM FILES
			BabelModel model = new BabelModel();
			model.AddFile("de",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\german.txt");
			model.AddFile("en",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\english.txt");
			model.AddFile("fr",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\french.txt");
			model.AddFile("it",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\italian.txt");
			model.AddFile("es",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\spanish.txt");
            model.SaveToFile(@"C:\src\DialogueMaster\DialogueMaster.Babel\models\small.model");
            

            model.AddFile("pt", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\portuguese.txt");
            model.AddFile("nl", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\dutch.txt");
			model.AddFile("sv",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\swedish.txt");
            model.AddFile("no", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\norwegian.txt");
            model.AddFile("da", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\danish.txt");
			model.SaveToFile(@"C:\src\DialogueMaster\DialogueMaster.Babel\models\\default.model");

            model.AddFile("ru", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\russian.txt");
            model.AddFile("el", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\greek.txt");
            model.AddFile("tr", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\turkish.txt");
            model.AddFile("cs", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\czech.txt");
			model.AddFile("pl",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\polish.txt");
			model.AddFile("is",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\icelandic.txt");
			model.AddFile("fi",@"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\finnish.txt");
            model.AddFile("zh", @"C:\src\DialogueMaster\DialogueMaster.Babel\Resources\chinese.zh.txt");
			model.SaveToFile(@"C:\src\DialogueMaster\DialogueMaster.Babel\models\\large.model");

			// System.Console.In.ReadLine();

			model = BabelModel.LoadFromFile(@"C:\src\DialogueMaster\DialogueMaster.Babel\models\large.model");

			TimeSpan duration = DateTime.Now-start;
			System.Console.Out.WriteLine("Read "+model.Count.ToString()+" languages in "+duration.TotalMilliseconds+"ms" );
			/*
			for(int i=0;i<100000;i++)
			{
				model.ClassifyText("Das ist ein Test text");
				model.ClassifyText("This is a simple test text");
			}
			*/

			System.Console.Out.WriteLine("Das ist ein Test text");
			System.Console.Out.WriteLine(model.ClassifyText("Das ist ein Test text"));

			System.Console.Out.WriteLine("This is a simple test text");
			System.Console.Out.WriteLine(model.ClassifyText("This is a simple test text"));
			System.Console.Out.WriteLine("This is a simple test text / Das ist ein Test text");
			System.Console.Out.WriteLine(model.ClassifyText("This is a simple test text / Das ist ein Test text"));

			System.Console.Out.WriteLine("Il a en outre voulu rassurer les princes de l'Eglise sur son intention de développer le gouvernement collégial de l'Eglise, c'est-à-dire de faire participer les prélats à la prise de décisions.");
			System.Console.Out.WriteLine(model.ClassifyText("Il a en outre voulu rassurer les princes de l'Eglise sur son intention de développer le gouvernement collégial de l'Eglise, c'est-à-dire de faire participer les prélats à la prise de décisions.") );

			System.Console.Out.WriteLine("Some people think this is a test / Glauben Sie es oder nicht, dies ist ein test / Il a en outre voulu rassurer les princes de l'Eglise sur son intention de développer.");
			System.Console.Out.WriteLine(model.ClassifyText("Some people think this is a test / Man mag glaube, das es ein test sei / Il a en outre voulu rassurer les princes de l'Eglise sur son intention de développer") );
			System.Console.Out.WriteLine("Enkelhet - snabb och enkel orderprocedur, förslag på relaterade produkter i samband med order samt möjlighet att återanvända tidigare inköpslistor");
			System.Console.Out.WriteLine(model.ClassifyText("Enkelhet - snabb och enkel orderprocedur, förslag på relaterade produkter i samband med order samt möjlighet att återanvända tidigare inköpslistor") );
			System.Console.Out.WriteLine("-------------------------------");
			System.Console.Out.WriteLine("Enter text to classify:");
			while(true)
			{
				string input = System.Console.ReadLine();
				if (input=="")
					break;
				System.Console.Out.WriteLine(model.ClassifyText(input));
			}
		}
	}
}
#endif