using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string SRC = "-src";
const string OUT = "-out";
const string NAME = "-n";
const string SVGS_FOLDER = "SVGs";
const string PREFIX = "Svg";
const string CONTRACTS_FOLDER = "contracts";

var regexClean = new Regex("xlink:href=\".*\"");
var regexStyle = new Regex(@"(style=)("")([\w|\d|,|\s|%|#|:|;|-]*)("")");
var regexStylePart = new Regex(@"([\w|-]*)(:)([%|#|\w|\d]*)");


var src = args.Where(m => m.StartsWith(SRC))
                .Select(m => m.Substring(SRC.Length + 1).Trim(' ', '"'))
                .First();
if (src == null)
{

    Console.WriteLine("Enter source folder");
    src = Console.ReadLine();
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
    Console.WriteLine("Enter destination folder");
    dest = Console.ReadLine();
}

var compName = args.Where(m => m.StartsWith(NAME))
                .Select(m => m.Substring(NAME.Length + 1).Trim(' ', '"').ToPascalCase())
                .First();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("SVG generator");
Console.ResetColor();

Console.WriteLine($"\t{SRC} {src}");
Console.WriteLine($"\t{OUT} {dest}");

if (!dest.EndsWith(compName) && !dest.EndsWith($"{PREFIX}{compName}"))
    dest = Path.Combine(dest, $"{PREFIX}{compName}");
var outDir = Path.GetFullPath(dest);
if (!Directory.Exists(outDir))
    Directory.CreateDirectory(outDir);

StringBuilder enumesBody = new StringBuilder();
StringBuilder imports = new StringBuilder();
StringBuilder switchs = new StringBuilder();
StringBuilder stories = new StringBuilder();
StringBuilder wicons = new StringBuilder();
StringBuilder isIn = new StringBuilder();
List<string> fileNames = new();


foreach (var file in Directory.GetFiles(src, "*.svg"))
{
    GenerateSvg(file);
}

string enums = File.ReadAllText("ENUM_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{name}}", compName)
    .Replace("{{body}}", enumesBody.ToString());
string enumsFile = Path.Combine(outDir, $"{PREFIX}{compName}List.ts");
File.WriteAllText(enumsFile, enums);

string isInEnums = File.ReadAllText("IS_IN_LIST_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{name}}", compName)
    .Replace("{{body}}", isIn.ToString());
string isInEnumsFile = Path.Combine(outDir, $"isIn{PREFIX}{compName}List.ts");
File.WriteAllText(isInEnumsFile, isInEnums);


string compRaw = File.ReadAllText("COMP_RAW_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{name}}", compName)
    .Replace("{{imports}}", imports.ToString())
    .Replace("{{switch}}", switchs.ToString());
string compRawFile = Path.Combine(outDir, $"W{PREFIX}{compName}Raw.tsx");
File.WriteAllText(compRawFile, compRaw);


string comp = File.ReadAllText("COMP_STYLE_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{name}}", compName);
string compFile = Path.Combine(outDir, $"W{PREFIX}{compName}.ts");
File.WriteAllText(compFile, comp);

string allstories = File.ReadAllText("STORYBOOK_ALL_TEMPLATE.txt")
                        .Replace("{{stories}}", wicons.ToString());

string storybookItems = File.ReadAllText("STORYBOOK_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{size}}", "30")
    .Replace("{{type}}", "Items")
    .Replace("{{name}}", compName)
    .Replace("{{stories}}", stories.ToString());
string storybookItemsFile = Path.Combine(outDir, $"W{compName}.items.stories.tsx");
File.WriteAllText(storybookItemsFile, storybookItems);

string storybookAll = File.ReadAllText("STORYBOOK_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{size}}", "10")
    .Replace("{{type}}", "All")
    .Replace("{{name}}", compName)
    .Replace("{{stories}}", allstories.ToString());
string storybookAllFile = Path.Combine(outDir, $"W{compName}.all.stories.tsx");
File.WriteAllText(storybookAllFile, storybookAll);

string index = File.ReadAllText("INDEX_TEMPLATE.txt")
    .Replace("{{prefix}}", PREFIX)
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


string indexOfContracts = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "index.txt"))
    .Replace("{{name}}", compName);
string indexOfContractsFile = Path.Combine(contractsDir, $"index.ts");
File.WriteAllText(indexOfContractsFile, indexOfContracts);

string IWSvgProps = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "IWSvgProps.txt"))
    .Replace("{{name}}", compName);
string IWSvgPropsFile = Path.Combine(contractsDir, $"IW{compName}SvgProps.ts");
File.WriteAllText(IWSvgPropsFile, IWSvgProps);

string IWSvgSafeProps = File.ReadAllText(Path.Combine(CONTRACTS_FOLDER, "IWSvgSafeProps.txt"))
    .Replace("{{prefix}}", PREFIX)
    .Replace("{{name}}", compName)
    .Replace("{{names}}", string.Join(" | ", fileNames.Select(m => $"'{m}'")));
string IWSvgSafePropsFile = Path.Combine(contractsDir, $"IW{compName}SvgSafeProps.ts");
File.WriteAllText(IWSvgSafePropsFile, IWSvgSafeProps);


void GenerateSvg(string path)
{
    string fileName = Path.GetFileNameWithoutExtension(path)
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace(" ", "")
                        .Trim()
                        .ToPascalCase();
    fileNames.Add(fileName);
    //string nameLower = fileName.ToCamelCase();
    string nameUpper = fileName.ToPascalCase();
    //string nameSCREAM = fileName.ToSCREAMING();
    string dir = Path.Combine(outDir, SVGS_FOLDER, nameUpper);
    if (Directory.Exists(dir))
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
        if (!element.StartsWith("<style"))
        {
            content.AppendLine(element);
        }
    }
    string raw = rawTemplate.Replace("{{body}}", content.ToString());

    string rawFile = Path.Combine(dir, $"{nameUpper}Raw.tsx");
    File.WriteAllText(rawFile, raw);
    string svgIndexFile = Path.Combine(dir, $"index.ts");
    string svgIndex = File.ReadAllText("SVG_INDEX_TEMPLATE.txt")
                            .Replace("{{name}}", nameUpper);
    File.WriteAllText(svgIndexFile, svgIndex);

    enumesBody.AppendLine($"{nameUpper} = \"{nameUpper}\",");
    imports.AppendLine($"import {{ {nameUpper} }} from './{SVGS_FOLDER}/{nameUpper}/{nameUpper}';");
    switchs.AppendLine($"{{icon === {PREFIX}{compName}List.{nameUpper} && <{nameUpper} className='svg' {{...props}} />}}");


    string story = File.ReadAllText("STORYBOOK_ELEMENT_TEMPLATE.txt")
                            .Replace("{{prefix}}", PREFIX)
                            .Replace("{{name}}", nameUpper)
                            .Replace("{{comp-name}}", compName);
    string storyTemplate = File.ReadAllText("STORYBOOK_ITEM_TEMPLATE.txt")
                            .Replace("{{name}}", nameUpper)
                            .Replace("{{story}}", story);
    stories.AppendLine(storyTemplate);
    wicons.AppendLine(story);

    isIn.AppendLine($"if (candidate === {PREFIX}{compName}List.{nameUpper}) return true;");
}


