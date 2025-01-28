using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using static GenerateAggregateRoot;

namespace bapsisCodeGenerator
{
    public class GenerateDbScript
    {
        private readonly string _modelPath;
        private readonly string _modelName;
        private readonly string _developerName;
        private readonly string _scriptDate;
        private readonly bool _hasMultiLanguageSupport;
        private readonly string _baseScriptPath;
        private readonly List<PropertyInfo> _properties;
        private bool _hasAuditEntity;
        private readonly List<RelationshipInfo> _relationships;

        public class PropertyInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsNullable { get; set; }
        }

        public GenerateDbScript(string modelPath, string modelName, string developerName,
            string scriptDate, bool hasMultiLanguageSupport)
        {
            _modelPath = modelPath;
            _modelName = modelName;
            _developerName = developerName;
            _scriptDate = scriptDate;
            _hasMultiLanguageSupport = hasMultiLanguageSupport;
            _properties = new List<PropertyInfo>();
            _relationships = new List<RelationshipInfo>();

            // Script dosyası için base path oluştur
            var projectDir = new DirectoryInfo(modelPath).Parent.Parent.Parent.Parent;
            _baseScriptPath = Path.Combine(projectDir.FullName, "Bapsis.Api.Data", "DbScripts");

            // Model içeriğini analiz et
            AnalyzeModelContent();
        }

        private void AnalyzeModelContent()
        {
            string modelContent = File.ReadAllText(_modelPath);

            // AuditEntity kontrolü
            _hasAuditEntity = modelContent.Contains(": AuditEntity") || modelContent.Contains(":AuditEntity");

            // Property'leri parse et
            var propertyRegex = new Regex(@"public\s+(\w+[\?]?)\s+(\w+)\s*{\s*get;");
            var propertyMatches = propertyRegex.Matches(modelContent);

            foreach (Match match in propertyMatches)
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                var isNullable = type.EndsWith("?");

                // Id ve audit alanlarını hariç tut
                if (name != "Id" &&
                    !IsAuditField(name))
                {
                    _properties.Add(new PropertyInfo
                    {
                        Type = type.TrimEnd('?'),
                        Name = name,
                        IsNullable = isNullable
                    });
                }
            }

            // İlişkileri analiz et
            var relationshipRegex = new Regex(@"public\s+virtual\s+(\w+)\s+(\w+)\s*{\s*get;\s*set;\s*}");
            var relationshipMatches = relationshipRegex.Matches(modelContent);

            foreach (Match match in relationshipMatches)
            {
                var relatedType = match.Groups[1].Value;
                var propertyName = match.Groups[2].Value;

                _relationships.Add(new RelationshipInfo
                {
                    RelatedType = relatedType,
                    Name = propertyName,
                    Type = "virtual",
                    IsReadOnly = false // navigationlar için varsayılan olarak false
                });
            }
        }

        private bool IsAuditField(string fieldName)
        {
            return new[] { "CreateUserId", "CreateDate", "ModifyUserId", "ModifyDate" }
                .Contains(fieldName);
        }

        public void Generate()
        {
            Directory.CreateDirectory(_baseScriptPath);

            string scriptFileName = $"Script_{_developerName}_{_scriptDate}.cs";
            string scriptFilePath = Path.Combine(_baseScriptPath, scriptFileName);

            var scriptContent = GenerateScriptContent();
            File.WriteAllText(scriptFilePath, scriptContent);

            Console.WriteLine($"DB Script başarıyla oluşturuldu: {scriptFilePath}");
        }

        private string GenerateScriptContent()
        {
            var sb = new StringBuilder();

            // Script header
            sb.AppendLine("using FluentMigrator;");
            sb.AppendLine();
            sb.AppendLine("namespace Bapsis.Api.Data.DbScripts;");
            sb.AppendLine();
            sb.AppendLine("[Tags(\"Bapsis\")]");
            sb.AppendLine($"[Migration({_scriptDate})]");
            sb.AppendLine($"public class Script_{_developerName}_{_scriptDate} : ForwardOnlyMigration");
            sb.AppendLine("{");
            sb.AppendLine("    public override void Up()");
            sb.AppendLine("    {");

            // Ana tablo için SQL
            sb.AppendLine($"        Execute.Sql(@\"CREATE TABLE IF NOT EXISTS public.\"\"{_modelName}s\"\"");
            sb.AppendLine("                (");

            // Primary Key
            sb.AppendLine($"                   \"\"Id\"\"               INTEGER NOT NULL CONSTRAINT \"\"PK_{_modelName}s\"\" PRIMARY KEY,");

            // Diğer property'ler
            foreach (var prop in _properties)
            {
                string sqlType = GetPostgreSqlType(prop.Type);
                string nullableStr = prop.IsNullable ? "" : " NOT NULL";

                sb.AppendLine($"                   \"\"{prop.Name}\"\"            {sqlType}{nullableStr},");
            }

            // Audit alanları
            if (_hasAuditEntity)
            {
                sb.AppendLine("                   \"\"CreateUserId\"\"     TEXT,");
                sb.AppendLine("                   \"\"CreateDate\"\"       TIMESTAMP NOT NULL,");
                sb.AppendLine("                   \"\"ModifyUserId\"\"     TEXT,");
                if (_relationships.Any())
                {
                    sb.AppendLine("                   \"\"ModifyDate\"\"       TIMESTAMP,");
                }
                else
                {
                    sb.AppendLine("                   \"\"ModifyDate\"\"       TIMESTAMP");
                }
            }

            // Foreign key constraint'leri
            if (_relationships.Any())
            {
                foreach (var relationship in _relationships)
                {
                    string fkPropertyName = $"{relationship.Name}Id";
                    string constraintName = $"FK_{_modelName}s_{relationship.RelatedType}s_{relationship.Name}";

                    if (relationship == _relationships.Last())
                    {
                        sb.AppendLine($"                   CONSTRAINT \"\"{constraintName}\"\"");
                        sb.AppendLine($"                       FOREIGN KEY (\"\"{fkPropertyName}\"\")");
                        sb.AppendLine($"                       REFERENCES public.\"\"{relationship.RelatedType}s\"\" (\"\"Id\"\"){(relationship.RelatedType == "Project" ? " ON DELETE CASCADE" : "")}");
                    }
                    else
                    {
                        sb.AppendLine($"                   CONSTRAINT \"\"{constraintName}\"\"");
                        sb.AppendLine($"                       FOREIGN KEY (\"\"{fkPropertyName}\"\")");
                        sb.AppendLine($"                       REFERENCES public.\"\"{relationship.RelatedType}s\"\" (\"\"Id\"\"){(relationship.RelatedType == "Project" ? " ON DELETE CASCADE" : "")},");
                    }
                }
            }

            sb.AppendLine("                );");
            sb.AppendLine("        \");");

            // MultiLanguage tablosu için SQL
            if (_hasMultiLanguageSupport)
            {
                sb.AppendLine();
                sb.AppendLine($"        Execute.Sql(@\"CREATE TABLE IF NOT EXISTS public.\"\"{_modelName}Languages\"\"");
                sb.AppendLine("                (");
                sb.AppendLine($"                   \"\"CoreId\"\"         INTEGER NOT NULL CONSTRAINT \"\"FK_{_modelName}Languages_{_modelName}s_CoreId\"\"");
                sb.AppendLine($"                                                   REFERENCES public.\"\"{_modelName}s\"\" (\"\"Id\"\") ON DELETE CASCADE,");
                sb.AppendLine("                   \"\"Language\"\"       TEXT NOT NULL,");
                sb.AppendLine("                   \"\"Name\"\"           TEXT NOT NULL,");
                sb.AppendLine("                   \"\"Description\"\"    TEXT,");

                if (_hasAuditEntity)
                {
                    sb.AppendLine("                   \"\"CreateUserId\"\"   TEXT,");
                    sb.AppendLine("                   \"\"CreateDate\"\"     TIMESTAMP NOT NULL,");
                    sb.AppendLine("                   \"\"ModifyUserId\"\"   TEXT,");
                    sb.AppendLine("                   \"\"ModifyDate\"\"     TIMESTAMP,");
                }

                sb.AppendLine($"                   CONSTRAINT \"\"PK_{_modelName}Languages\"\" PRIMARY KEY (\"\"CoreId\"\", \"\"Language\"\")");
                sb.AppendLine("                );");
                sb.AppendLine("        \");");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GetPostgreSqlType(string csharpType)
        {
            return csharpType.ToLower() switch
            {
                "int" => "INTEGER",
                "long" => "BIGINT",
                "string" => "TEXT",
                "datetime" => "TIMESTAMP",
                "bool" => "BOOLEAN",
                "decimal" => "NUMERIC",
                "double" => "DOUBLE PRECISION",
                "float" => "REAL",
                "guid" => "UUID",
                _ => "TEXT"  // Varsayılan tip
            };
        }
    }
}