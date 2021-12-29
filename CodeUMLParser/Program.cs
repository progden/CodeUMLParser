using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
			var baseFolder = "C:\\Users\\ccdru\\code\\hyt\\mega-expediting";
			var files = Directory.EnumerateFiles(baseFolder, "*.cs", SearchOption.AllDirectories);
			var list = files.Where(path =>
			{
				return !(path.Contains("\\Batches\\") ||
						path.Contains(".Test\\") ||
						path.Contains(".cshtml.cs") ||
						path.Contains("\\Migrations\\") ||
						path.Contains("\\obj\\") ||
						path.Contains("\\node_modules\\") ||
						path.Contains("\\ViewModel\\")
					)
					&& (
						path.Contains("mega-expediting\\mega-expediting") &&
						(
							path.ToUpper().Contains("SERVICE.CS") ||
							path.ToUpper().Contains("FACADE.CS") ||
							path.ToUpper().Contains("CONTROLLER.CS") ||
							path.ToUpper().Contains("MWARE.CS") ||
							path.ToUpper().Contains("UTIL.CS") ||
							path.ToUpper().Contains("DAO.CS") ||
							path.ToUpper().Contains("USERTOKENINFO.CS") ||
							path.ToUpper().Contains("FACTORY.CS") ||
							false
						)
					)
					;
			}).ToList();
			
			list.Sort();

			Console.WriteLine("@startuml");
			for (var index = 0; index < list.Count; index++)
			{
				var path = list[index];
				var text = File.ReadAllText(path);
				SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
				CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

				var clzzCollector = new ClassCollector();
				clzzCollector.Visit(root);
				
				// show(clzzCollector, path);

				foreach (var clzzName in clzzCollector.ClassList)
				{
					if (clzzName.StartsWith("I"))
					{
						Console.WriteLine($"interface {clzzName}");
					}
					else
					{
						Console.WriteLine($"class {clzzName}");
					}
					
					var baseList = clzzCollector.BaseList[clzzName];
					Console.ForegroundColor = ConsoleColor.Blue;
					if (baseList != null)
					{
						var types = baseList.Types;
						var baselist = types.Select(t => t.GetText().ToString().Trim());
						foreach (var _base in baselist)
						{
							if(_base.StartsWith("I"))
								Console.WriteLine($"{_base} <|.. {clzzName}");
							else
								Console.WriteLine($"{_base} <|-- {clzzName}");
						}
					}

					var properties = clzzCollector.Property[clzzName];
					Console.ForegroundColor = ConsoleColor.Magenta;
					if (properties.Count > 0)
					{
						foreach (var property in properties)
						{
							var ignore = new List<string> {"int", "string", "Func"};
							if(!ignore.Any(ptn => property.Type.ToString().StartsWith(ptn)))
								Console.WriteLine($"{clzzName} --> {property.Type}: Use");
						}
					}
					Console.ForegroundColor = ConsoleColor.Black;
				}
			}
			Console.WriteLine("@enduml");
        }

        private static void show(ClassCollector clzzCollector, string path)
        {
	        // 路徑
	        Console.WriteLine(path);

	        foreach (var clzzName in clzzCollector.ClassList)
	        {
		        Console.ForegroundColor = ConsoleColor.Green;
		        Console.WriteLine("類別: " + string.Join(",", clzzName));

		        var baseList = clzzCollector.BaseList[clzzName];
		        Console.ForegroundColor = ConsoleColor.Blue;
		        if (baseList != null)
		        {
			        var types = baseList.Types;
			        var enumerable = types.Select(t => t.GetText().ToString().Trim());
			        Console.WriteLine("繼承: " + string.Join(",", enumerable));
		        }
		        else
		        {
			        Console.WriteLine("繼承: 沒有");
		        }

		        Console.ForegroundColor = ConsoleColor.Red;
		        var propList = clzzCollector.Property[clzzName];
		        if (propList.Count > 0)
		        {
			        foreach (var prop in propList)
			        {
				        Console.WriteLine("Prop: " + prop.Type.GetText().ToString());
			        }
		        }
		        else
		        {
			        Console.WriteLine("Prop: 沒有");
		        }
	        }
			Console.ForegroundColor = ConsoleColor.Black;
        }
    }

	class ClassCollector : CSharpSyntaxWalker
	{
		private string ClassName { set; get; }

		public List<string> ClassList { get; } = new();
		public Dictionary<string, BaseListSyntax> BaseList { get; } = new ();
		public Dictionary<string, ICollection<PropertyDeclarationSyntax>> Property { get; } = new ();

		public override void VisitClassDeclaration(ClassDeclarationSyntax node)
		{
			this.ClassName = node.Identifier.Text;
			this.ClassList.Add(this.ClassName);
			this.BaseList.Add(this.ClassName, node.BaseList);
			if (!this.Property.ContainsKey(this.ClassName))
			{
				this.Property[this.ClassName] = new List<PropertyDeclarationSyntax>();
			}
			base.VisitClassDeclaration(node);
		}
		public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
		{
			this.ClassName = node.Identifier.Text;
			this.ClassList.Add(this.ClassName);
			this.BaseList.Add(this.ClassName, node.BaseList);
			if (!this.Property.ContainsKey(this.ClassName))
			{
				this.Property[this.ClassName] = new List<PropertyDeclarationSyntax>();
			}
			base.VisitInterfaceDeclaration(node);
		}


		public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
		{
			this.Property[this.ClassName].Add(node);
		}
	}
}