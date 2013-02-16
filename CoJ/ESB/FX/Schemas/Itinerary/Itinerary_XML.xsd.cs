namespace CoJ.ESB.FX.Schemas.Itinerary {
    using Microsoft.XLANGs.BaseTypes;
    
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.BizTalk.Schema.Compiler", "3.0.1.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [SchemaType(SchemaTypeEnum.Document)]
    [Schema(@"http://coj.esb/schemas/itinerary",@"Itinerary")]
    [System.SerializableAttribute()]
    [SchemaRoots(new string[] {@"Itinerary"})]
    public sealed class Itinerary_XML : Microsoft.XLANGs.BaseTypes.SchemaBase {
        
        [System.NonSerializedAttribute()]
        private static object _rawSchema;
        
        [System.NonSerializedAttribute()]
        private const string _strSchema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema xmlns=""http://coj.esb/schemas/itinerary"" xmlns:b=""http://schemas.microsoft.com/BizTalk/2003"" attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" targetNamespace=""http://coj.esb/schemas/itinerary"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Itinerary"">
    <xs:complexType>
      <xs:sequence>
        <xs:element maxOccurs=""unbounded"" name=""Resolver"">
          <xs:complexType>
            <xs:sequence>
              <xs:element maxOccurs=""unbounded"" name=""ResolverGroup"">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element maxOccurs=""unbounded"" name=""EndPoint"">
                      <xs:complexType>
                        <xs:attribute name=""protocol"" type=""xs:string"" use=""required"" />
                        <xs:attribute name=""location"" type=""xs:string"" use=""required"" />
                        <xs:attribute name=""requireTransformation"" type=""xs:string"" />
                        <xs:attribute name=""transformationXSL"" type=""xs:string"" />
                        <xs:attribute name=""requireRulesPolicy"" type=""xs:string"" />
                        <xs:attribute name=""rulesPolicyName"" type=""xs:string"" />
                        <xs:attribute name=""requireFFDasm"" type=""xs:string"" />
                        <xs:attribute name=""ffDasmSchemaName"" type=""xs:string"" />
                      </xs:complexType>
                    </xs:element>
                  </xs:sequence>
                  <xs:attribute name=""routingType"" type=""xs:string"" />
                  <xs:attribute name=""routingPropertyName"" type=""xs:string"" />
                  <xs:attribute name=""routingPropertyNamespace"" type=""xs:string"" />
                  <xs:attribute name=""routingPropertyValue"" type=""xs:string"" />
                  <xs:attribute name=""route"" type=""xs:string"" />
                </xs:complexType>
              </xs:element>
            </xs:sequence>
            <xs:attribute name=""identifier"" type=""xs:string"" use=""required"" />
          </xs:complexType>
        </xs:element>
      </xs:sequence>
      <xs:attribute name=""version"" type=""xs:decimal"" use=""required"" />
      <xs:attribute name=""name"" type=""xs:string"" use=""required"" />
    </xs:complexType>
  </xs:element>
</xs:schema>";
        
        public Itinerary_XML() {
        }
        
        public override string XmlContent {
            get {
                return _strSchema;
            }
        }
        
        public override string[] RootNodes {
            get {
                string[] _RootElements = new string [1];
                _RootElements[0] = "Itinerary";
                return _RootElements;
            }
        }
        
        protected override object RawSchema {
            get {
                return _rawSchema;
            }
            set {
                _rawSchema = value;
            }
        }
    }
}
