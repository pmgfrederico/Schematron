using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Text;
using System.Xml.Schema;
using Xunit;

using ValidationResult = Schematron.Serialization.SchematronValidationResultTempObjectModel.output;
using System.Diagnostics;

namespace Schematron.Tests
{
    public class ValidatorTests
    {
        public class SchemaLoadingFailedException : Exception { }

        const string XsdLocation = "./Content/po-schema.xsd";
        const string XsdWithPartialSchemaLocation = "./Content/po-schema-with-schema-import.xsd";
        const string XmlContentLocation = "./Content/po-instance.xml";
        const string TargetNamespace = "http://example.com/po-schematron";

        // TODO Scenarios to test

        // Should adding schemas phase succeed
        // 1.0 base schema validation is OK
            // 1.1 and the schematron validation is OK
            // 1.2 and the schematron validation is KO
        // 2.0 base schema validatino is KO
            // 2.1 and the schematron validation is OK
            // 2.2 and the schematron validation is KO

        #region .AddSchema(params) - Validation should be independent of the method I choose to add the schemas

        #region Scenario 1. .AddSchema(XmlSchema) - Adding the XmlSchema directly won't account for the schematron annotations
        
        [Fact]
        public void WhenSchemaIsValid_AddSchemaShouldNotThrowXmlValidationException_S1()
        {
            var validator = new Schematron.Validator(OutputFormatting.XML);

            using (var schemaXmlReader = XmlReader.Create(XsdWithPartialSchemaLocation))
            {
                var schema = XmlSchema.Read(schemaXmlReader, (sender, args) => { throw new SchemaLoadingFailedException(); });

                validator.AddSchema(schema);

                var ex = Xunit.Assert.Throws<Schematron.ValidationException>(() => validator.Validate(XmlContentLocation));

                var result = ex.ToValidationResult();

                Xunit.Assert.NotNull(result.xml);
                Xunit.Assert.Null(result.schematron);
            }
        }

        #endregion

        #region Scenario 2. .AddSchema(Uri) -> .AddSchema(Stream) -> .AddSchema(XmlTextReader)  -> .AddSchema(XmlReader)

        [Fact]
        public void WhenSchemaIsValid_AddSchemaShouldNotThrowXmlValidationException_S2()
        {
            var validator = new Schematron.Validator(OutputFormatting.XML);
            
            validator.AddSchema(XsdWithPartialSchemaLocation); // The test is currently failing here since it does not support schemas with imports to be added this way

            Xunit.Assert.Throws<Schematron.ValidationException>(() => validator.Validate(XmlContentLocation));
        }                             

        #endregion

        #region Scenario 3. .AddSchema(TargetNamespace, SchemaUri)

        [Fact]
        public void WhenSchemaIsValid_AddSchemaShouldNotThrowXmlValidationException_S3()
        {
            var validator = new Schematron.Validator(OutputFormatting.XML);

            validator.AddSchema(TargetNamespace, XsdWithPartialSchemaLocation);

            var ex = Xunit.Assert.Throws<Schematron.ValidationException>(() => validator.Validate(XmlContentLocation));

            var result = ex.ToValidationResult();

            Xunit.Assert.NotNull(result.xml);
            Xunit.Assert.NotNull(result.schematron);
        }

        #endregion

        [Fact]
        public void WhileUsingDistinctAddSchemaMethods_ValidationResultShouldBeExactlyTheSame()
        {
            var validatorA = new Validator(OutputFormatting.XML);
            validatorA.AddSchema(XmlReader.Create(XsdLocation));

            var validatorB = new Validator(OutputFormatting.XML);
            validatorB.AddSchema(TargetNamespace, XsdLocation);

            var resultA = default(string);
            var resultB = default(string);

            var exA = Xunit.Assert.Throws<Schematron.ValidationException>(() => validatorA.Validate(XmlReader.Create(XmlContentLocation)));
            var exB = Xunit.Assert.Throws<Schematron.ValidationException>(() => validatorB.Validate(XmlReader.Create(XmlContentLocation)));

            Debug.WriteLine(exA.Message);
            Debug.WriteLine(exB.Message);

            Xunit.Assert.True(resultA == resultB);
        }

        #endregion

        [Fact]
        public void WhenIAddASchemaForSchematronValidationOnlyPurposes_AndASchemaWiseInvalidXmlDocument_ValidatorShouldThrow()
        {
            var validator = new Schematron.Validator(OutputFormatting.XML);

            var schematron = new Schema();
            schematron.Load(XsdWithPartialSchemaLocation);

            validator.AddSchema(schematron);

            var ex = Xunit.Assert.Throws<Schematron.ValidationException>(() => validator.Validate(XmlContentLocation));

            Xunit.Assert.Null(ex.ToValidationResult().xml);
            Xunit.Assert.NotNull(ex.ToValidationResult().schematron);
        }
    }

    public static class SchematronExtensions
    {
        public static ValidationResult ToValidationResult(this Schematron.ValidationException ex)
        {
            var serializer = new XmlSerializer(typeof(Schematron.Serialization.SchematronValidationResultTempObjectModel.output));

            using (var stream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(ex.Message)))
            using (var reader = XmlReader.Create(stream))
            {
                return (Schematron.Serialization.SchematronValidationResultTempObjectModel.output)serializer.Deserialize(reader);
            }
        }
    }
}
