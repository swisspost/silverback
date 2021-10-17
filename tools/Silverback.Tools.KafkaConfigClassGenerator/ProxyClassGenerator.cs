﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Silverback.Tools.KafkaConfigClassGenerator
{
    internal sealed class ProxyClassGenerator
    {
        private readonly Type _proxiedType;

        private readonly string _generatedClassName;

        private readonly string? _baseClassName;

        private readonly string? _clientConfigType;

        private readonly CodeDomProvider _codeDomProvider = CodeDomProvider.CreateProvider("C#");

        private readonly string _xmlDocumentationPath;

        private readonly bool _generateNamespace;

        private readonly string? _proxiedTypeName;

        private readonly StringBuilder _builder = new();

        private XmlDocument? _xmlDoc;

        public ProxyClassGenerator(
            Type proxiedType,
            string generatedClassName,
            string? baseClassName,
            string? clientConfigType,
            string xmlDocumentationPath,
            bool generateNamespace)
        {
            _proxiedType = proxiedType;
            _proxiedTypeName = _proxiedType.FullName;

            _generatedClassName = generatedClassName;
            _baseClassName = baseClassName;
            _clientConfigType = clientConfigType;
            _xmlDocumentationPath = xmlDocumentationPath;
            _generateNamespace = generateNamespace;
        }

        public string Generate()
        {
            GenerateHeading();
            MapProperties();
            GenerateFooter();

            return _builder.ToString();
        }

        private void GenerateHeading()
        {
            if (_generateNamespace)
            {
                _builder.AppendLine("namespace Silverback.Messaging.Proxies");
                _builder.AppendLine("{");
            }

            _builder.AppendLine("    /// <summary>");
            _builder.AppendLine($"    ///     Wraps the <see cref=\"{_proxiedTypeName}\" />.");
            _builder.AppendLine("    /// </summary>");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"SA1649\", Justification = \"Autogenerated all at once\")]");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"SA1402\", Justification = \"Autogenerated all at once\")]");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"CA1200\", Justification = \"Summary copied from wrapped class\")]");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"SA1623\", Justification = \"Summary copied from wrapped class\")]");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"SA1629\", Justification = \"Summary copied from wrapped class\")]");
            _builder.AppendLine(
                "    [SuppressMessage(\"\", \"CA1044\", Justification = \"Accessors generated according to wrapped class\")]");

            if (_baseClassName == null)
            {
                _builder.AppendLine(
                    $"    public abstract class {_generatedClassName} : IValidatableEndpointSettings");
                _builder.AppendLine("    {");
                _builder.AppendLine(
                    "        internal static readonly ConfigurationDictionaryEqualityComparer<string, string> ConfluentConfigEqualityComparer = new();");
                _builder.AppendLine();
                _builder.AppendLine("        /// <summary>");
                _builder.AppendLine(
                    $"        ///     Initializes a new instance of the <see cref=\"{_generatedClassName}\" /> class.");
                _builder.AppendLine("        /// </summary>");
                _builder.AppendLine("        /// <param name=\"confluentConfig\">");
                _builder.AppendLine(
                    "        ///     The <see cref=\"Confluent.Kafka.ClientConfig\"/> to wrap.");
                _builder.AppendLine("        /// </param>");
                _builder.AppendLine($"        protected {_generatedClassName}(Confluent.Kafka.ClientConfig confluentConfig)");
                _builder.AppendLine("        {");
                _builder.AppendLine("            ConfluentConfig = confluentConfig;");
                _builder.AppendLine("        }");
                _builder.AppendLine();
            }
            else
            {
                _builder.AppendLine($"    public abstract class {_generatedClassName} : {_baseClassName}");
                _builder.AppendLine("    {");
                _builder.AppendLine("        /// <summary>");
                _builder.AppendLine(
                    $"        ///     Initializes a new instance of the <see cref=\"{_generatedClassName}\" /> class.");
                _builder.AppendLine("        /// </summary>");
                _builder.AppendLine("        /// <param name=\"clientConfig\">");
                _builder.AppendLine(
                    $"        ///     The <see cref=\"Confluent.Kafka.ClientConfig\" /> to be used to initialize the <see cref=\"{_clientConfigType}\" />.");
                _builder.AppendLine("        /// </param>");
                _builder.AppendLine(
                    $"        protected {_generatedClassName}(Confluent.Kafka.ClientConfig? clientConfig = null)");
                _builder.AppendLine(
                    $"            : base(clientConfig != null ? new {_clientConfigType}(clientConfig.Clone()) : new {_clientConfigType}())");
                _builder.AppendLine("        {");
                _builder.AppendLine("        }");
            }
        }

        private void MapProperties()
        {
            foreach (var property in GetProperties())
            {
                var propertyType = GetPropertyTypeString(property.PropertyType);
                WriteSummary(property);

                _builder.AppendLine($"        public {propertyType} {property.Name}");
                _builder.AppendLine("        {");

                if (property.GetGetMethod() != null)
                    _builder.AppendLine($"            get => ConfluentConfig.{property.Name};");

                if (property.Name == "DeliveryReportFields")
                {
                    _builder.AppendLine("            set");
                    _builder.AppendLine("            {");
                    _builder.AppendLine("                if (value != null)");
                    _builder.AppendLine($"                    ConfluentConfig.{property.Name} = value;");
                    _builder.AppendLine("            }");
                }
                else if (property.GetSetMethod() != null)
                {
                    _builder.AppendLine($"            set => ConfluentConfig.{property.Name} = value;");
                }

                _builder.AppendLine("        }");
                _builder.AppendLine();
            }
        }

        private PropertyInfo[] GetProperties()
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            if (_baseClassName != null)
                bindingFlags |= BindingFlags.DeclaredOnly;

            return _proxiedType.GetProperties(bindingFlags);
        }

        private void GenerateFooter()
        {
            if (_baseClassName == null)
            {
                _builder.AppendLine("        /// <summary>");
                _builder.AppendLine(
                    "        ///     Gets the <see cref=\"Confluent.Kafka.ClientConfig\" /> instance being wrapped.");
                _builder.AppendLine("        /// </summary>");
                _builder.AppendLine("        protected Confluent.Kafka.ClientConfig ConfluentConfig { get; }");
                _builder.AppendLine();
                _builder.AppendLine("        internal Confluent.Kafka.ClientConfig GetConfluentConfig() => ConfluentConfig;");
                _builder.AppendLine();
                _builder.AppendLine("        /// <inheritdoc cref=\"IValidatableEndpointSettings.Validate\" />");
                _builder.AppendLine("        public abstract void Validate();");
            }
            else
            {
                _builder.AppendLine("        /// <summary>");
                _builder.AppendLine(
                    "        ///     Gets the <see cref=\"Confluent.Kafka.ClientConfig\" /> instance being wrapped.");
                _builder.AppendLine("        /// </summary>");
                _builder.AppendLine($"        protected new {_clientConfigType} ConfluentConfig => ({_clientConfigType})base.ConfluentConfig;");
                _builder.AppendLine();
                _builder.AppendLine($"        internal new {_clientConfigType} GetConfluentConfig() => ConfluentConfig;");
            }

            _builder.AppendLine("    }");

            if (_generateNamespace)
                _builder.Append('}');
        }

        private string GetPropertyTypeString(Type propertyType)
        {
            var nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
                return GetTypeName(nullableType) + "?";

            return GetTypeName(propertyType);
        }

        private string GetTypeName(Type type)
        {
            var typeReferenceExpression = new CodeTypeReferenceExpression(new CodeTypeReference(type));

            using var writer = new StringWriter();

            _codeDomProvider.GenerateCodeFromExpression(
                typeReferenceExpression,
                writer,
                new CodeGeneratorOptions());
            return writer.GetStringBuilder().ToString();
        }

        private void WriteSummary(PropertyInfo propertyInfo)
        {
            if (_xmlDoc == null)
                LoadXmlDoc();

            var path = "P:" + propertyInfo.DeclaringType?.FullName + "." + propertyInfo.Name;
            var node = _xmlDoc?.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");

            if (node == null)
                return;

            foreach (var line in node.InnerXml.Split("\r\n", StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("default: ", StringComparison.Ordinal))
                    _builder.AppendLine($"        /// <br/><br/>{line}");
                else if (line.StartsWith("importance: ", StringComparison.Ordinal))
                    _builder.AppendLine($"        /// <br/>{line}");
                else
                    _builder.AppendLine($"        /// {line}");
            }

            WriteCustomRemarks(propertyInfo.Name);
        }

        private void LoadXmlDoc()
        {
            if (!File.Exists(_xmlDocumentationPath))
                return;

            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(_xmlDocumentationPath);
        }

        private void WriteCustomRemarks(string propertyName)
        {
            if (propertyName == "DeliveryReportFields")
            {
                _builder.AppendLine("            /// <remarks>");
                _builder.AppendLine(
                    "            ///     Silverback overrides this value by default setting it to &quot;key,status&quot; as an optimization,");
                _builder.AppendLine("            ///     since the other fields aren't used.");
                _builder.AppendLine("            /// </remarks>");
            }
        }
    }
}
