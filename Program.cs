using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string SRC = "-src";
const string OUT = "-out";
const string NAME = "-n";
const string SVGS_FOLDER = "SVGs";
const string CONTRACTS_FOLDER = "contracts";

var regexClean = new Regex("xlink:href=\".*\"");
var regexStyle = new Regex(@"(style=)("")([\w|\d|,|\s|%|#|:|;|-]*)("")");
var regexStylePart = new Regex(@"([\w|-]*)(:)([%|#|\w|\d]*)");


var src = args.Where(m => m.StartsWith(SRC))
                .Select(m => m.Substring(SRC.Length + 1).Trim(' ', '"'))
                .First();
if (src == null)
{
    Console.WriteLine($"{SRC} argument is missing");
    Console.WriteLine($"{SRC} stands for the source directory");
    return;
}

if (!Directory.Exists(src))
{ 
    Console.WriteLine($"[{src}] directory not exists");
    return;
}

var dest = args.Where(m => m.StartsWith(OUT))
                .Select(m => m.Substring(OUT.Length + 1).Trim(' ', '"'))
                .First();
if (dest == null)
{
    Console.WriteLine($"{OUT} argument is missing");
    Console.WriteLine($"{OUT} stands for the out directory, where the generated file should be");
    return;
}

var compName = args.Where(m => m.StartsWith(NAME))
                .Select(m => m.Substring(NAME.Length + 1).Trim(' ', '"').ToPascalCase())
                .First();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("SVG generator");
Console.ResetColor();

Console.WriteLine($"\t{SRC} {src}");
Console.WriteLine($"\t{OUT} {dest}");

if(!dest.EndsWith(compName) && !dest.EndsWith($"{compName}Icons"))
    dest = Path.Combine(dest, $"{compName}Icons");
var outDir = Path.GetFullPath(dest);
if(!Directory.Exists(outDir)) 
    Directory.CreateDirectory(outDir);

StringBuilder enumesBody = new StringBuilder();
StringBuilder imports = new StringBuilder();
StringBuilder switchs = new StringBuilder();
StringBuilder stories = new StringBuilder();
StringBuilder wicons = new StringBuilder();


foreach (var file in Directory.GetFiles(src, "*.svg"))
{
    GenerateSvg(file);
}

string enums = File.ReadAllText("ENUM_TEMPLATE.txt")
    .Replace("{{name}}", compName)
    .Replace("{{body}}", enumesBody.ToString());
string enumsFile = Path.Combine(outDir, $"{compName}IconsList.ts");
File.WriteAllText(enumsFile, enums);


string compRaw = File.ReadAllText("COMP_RAW_TEMPLATE.txt")
    .Replace("{{name}}", compName)
    .Replace("{{imports}}", imports.ToString())
    .Replace("{{switch}}", switchs.ToString());
string compRawFile = Path.Combine(outDir, $"W{compName}IconsRaw.tsx");
File.WriteAllText(compRawFile, compRaw);


string comp = File.ReadAllText("COMP_STYLE_TEMPLATE.txt")
    .Replace("{{name}}", compName);
string compFile = Path.Combine(outDir, $"W{compName}Icons.ts");
File.WriteAllText(compFile, comp);

string allstories = File.ReadAllText("STORYBOOK_ALL_TEMPLATE.txt")
                        .Replace("{{stories}}", wicons.ToString());

string storybookItems = File.ReadAllText("STORYBOOK_TEMPLATE.txt")
    .Replace("{{type}}", "Items")
    .Replace("{{name}}", compName)
    .Replace("{{stories}}", stories.ToString());
string storybookItemsFile = Path.Combine(outDir, $"W{compName}.items.stories.tsx");
File.WriteAllText(storybookItemsFile, storybookItems);

string storybookAll = File.ReadAllText("STORYBOOK_TEMPLATE.txt")
    .Replace("{{type}}", "All")
    .Replace("{{name}}", compName)
    .Replace("{{stories}}", allstories.ToString());
string storybookAllFile = Path.Combine(outDir, $"W{compName}.all.stories.tsx");
File.WriteAllText(storybookAllFile, storybookAll);

string index = File.ReadAllText("INDEX_TEMPLATE.txt")
    .Replace("{{name}}", compName);
string indexFile = Path.Combine(outDir, $"index.ts");
File.WriteAllText(indexFile, index);

string contractsDir = Path.Combine(outDir, CONTRACTS_FOLDER);
if (Directory.Exists(contractsDir))
    Directory.Delete(contractsDir, true);
Thread.Sleep(1);
Directory.CreateDirectory(contractsDir);

string guardSafeIcon = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "guardSafeIcon.txt"))
    .Replace("{{name}}", compName);
string guardSafeIconFile = Path.Combine(contractsDir, $"guardSafeIcon.ts");
File.WriteAllText(guardSafeIconFile, guardSafeIcon);

string guardUnsafeIcon = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "guardUnsafeIcon.txt"))
    .Replace("{{name}}", compName);
string guardUnsafeIconFile = Path.Combine(contractsDir, $"guardUnsafeIcon.ts");
File.WriteAllText(guardUnsafeIconFile, guardUnsafeIcon);

string indexOfContracts = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "index.txt"))
    .Replace("{{name}}", compName);
string indexOfContractsFile = Path.Combine(contractsDir, $"index.ts");
File.WriteAllText(indexOfContractsFile, indexOfContracts);

string IWSvgProps = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "IWSvgProps.txt"))
    .Replace("{{name}}", compName);
string IWSvgPropsFile = Path.Combine(contractsDir, $"IW{compName}SvgProps.ts");
File.WriteAllText(IWSvgPropsFile, IWSvgProps);

string IWSvgSafeProps = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "IWSvgSafeProps.txt"))
    .Replace("{{name}}", compName);
string IWSvgSafePropsFile = Path.Combine(contractsDir, $"IW{compName}SvgSafeProps.ts");
File.WriteAllText(IWSvgSafePropsFile, IWSvgSafeProps);

string IWSvgUnsafeProps = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "IWSvgUnsafeProps.txt"))
    .Replace("{{name}}", compName);
string IWSvgUnsafePropsFile = Path.Combine(contractsDir, $"IW{compName}SvgUnsafeProps.ts");
File.WriteAllText(IWSvgUnsafePropsFile, IWSvgUnsafeProps);



void GenerateSvg(string path)
{
    string fileName = Path.GetFileNameWithoutExtension(path);
    string nameLower = fileName.ToCamelCase();
    string nameUpper = fileName.ToPascalCase();
    string dir = Path.Combine(outDir, SVGS_FOLDER, nameUpper);
    if(Directory.Exists(dir))
        Directory.Delete(dir, true);
    Thread.Sleep(1);
    Directory.CreateDirectory(dir);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Generating [{nameUpper}]");
    Console.ResetColor();

    string style = File.ReadAllText("STYLE_TEMPLATE.txt").Replace("{{name}}", nameUpper);
    string styleFile = Path.Combine(dir, $"{nameUpper}.ts");
    File.WriteAllText(styleFile, style);

    XDocument doc = XDocument.Load(path);
    string viewBox = doc.Root?.Attribute("viewBox")?.Value ?? throw new Exception("viewBox is missing");

    string rawTemplate = File.ReadAllText("RAW_TEMPLATE.txt")
                            .Replace("{{name}}", nameUpper)
                            .Replace("{{viewBox}}", viewBox);
    StringBuilder content = new();
    foreach (XElement e in doc.Root.Elements())
    {
        string element = e.ToString()
                          .Replace("class=", "className=");
        element = regexClean.Replace(element, "");
        var styleMatch = regexStyle.Match(element);
        if (styleMatch.Success) 
        {
            string matchValue = styleMatch.Groups[3].Value;

            string[] ks = matchValue.Split(";", StringSplitOptions.RemoveEmptyEntries);
            
           
            string fix = string.Join(",", ks.Select(m =>
            {
                string k = regexStylePart.Match(m).Groups[1].Value.ToCamelCase();
                return regexStylePart.Replace(
                                        m,
                                        $"{k}$2\"$3\"");
            }));
            element = regexStyle.Replace(element, $"$1{{{{{fix}}}}}");
        }
        content.AppendLine(element);
    }
    string raw = rawTemplate.Replace("{{body}}", content.ToString());

    string rawFile = Path.Combine(dir, $"{nameUpper}Raw.tsx");
    File.WriteAllText(rawFile, raw);
    enumesBody.AppendLine($"{nameLower} = \"{fileName}\",");
    imports.AppendLine($"import {{ {nameUpper} }} from './{SVGS_FOLDER}/{nameUpper}/{nameUpper}';");
    switchs.AppendLine($"{{icon === {compName}IconsList.{nameLower} && <{nameUpper} className='svg' {{...props}} />}}");


    string story = $"<W{compName}Icons icon='{nameUpper}' {{...args}} />";
    wicons.AppendLine(story);
    string storyTemplate = File.ReadAllText("STORYBOOK_ITEM_TEMPLATE.txt")
                            .Replace("{{name}}", nameUpper)
                            .Replace("{{story}}", story);
    stories.AppendLine(storyTemplate);
}


