using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Collections;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;



namespace saml
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class SAMLAssertionCreator
	{
		
		public SAMLAssertionCreator()
		{
		}


        public string signXML(string strXml,  X509Certificate2 cert)
        {
            
            // Create a new XML document.
            XmlDocument doc = new XmlDocument();
            

            // Format the document to ignore white spaces.
            doc.PreserveWhitespace = true;

            // Load the passed XML file using it's name.
            doc.LoadXml(strXml);

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(doc);

            // Add the key to the SignedXml document. 
            try
            {
                signedXml.SigningKey = cert.PrivateKey;
            }
            catch (CryptographicException ce)
            {
                throw new SAMLAssertionCreationException("Problem accessing private key. Most likely related to security permissions. Try using winhttpcertcfg as described in readme.txt", ce);
            }


            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add a transformation to the reference.
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            // Set the ExclusiveNamespacesPrefixList property.        
            XmlDsigExcC14NTransform trns = new XmlDsigExcC14NTransform();
            trns.InclusiveNamespacesPrefixList = "";

            reference.AddTransform(trns);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Add an RSAKeyValue KeyInfo (optional; helps recipient find key to validate).

            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));

            signedXml.KeyInfo = keyInfo;


            // Compute the signature.
            try
            {
                signedXml.ComputeSignature();
            }
            catch (CryptographicException ce)
            {
                throw new SAMLAssertionCreationException("Problem generating signature. Make sure private key is loaded. Have you imported saml_dsa.cer instead of saml_dsa.pfx?", ce);
            }

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.PrependChild(doc.ImportNode(xmlDigitalSignature, true));


            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            return doc.InnerXml;			

        }

		public static char forDigit(int digit, int radix) 
		{
			if ((digit >= radix) || (digit < 0)) 
			{
				return '\0';
			}
			if ((radix < 2) || (radix > 36)) 
			{
				return '\0';
			}
			if (digit < 10) 
			{
				return (char)('0' + digit);
			}
			return (char)('a' - 10 + digit);
		}

        Random random = new Random();
		public String generateId()
		{
			StringBuilder id = new StringBuilder();
			byte [] abyte = new byte[32];
			
			do
				random.NextBytes(abyte);
			while((abyte[0] & 0xf) < 10);
			for(int i = 0; i < 32; i++)
				id.Append(forDigit(abyte[i] & 0xf, 16));

			return id.ToString();

		}

        public String createAssertion(String userId, String issuer, String recepient, String keyName)
        {
            return createAssertion(userId, issuer, recepient, keyName, 60000);
        }
		public String createAssertion(String userId, String issuer, String recepient, String keyName, double timeToLiveMS)
		{

            String assertionId = generateId();
            DateTime dt = DateTime.UtcNow;
            
			String time=dt.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            String expiryTime = dt.AddMilliseconds(timeToLiveMS).ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
			String assertionXml = "<saml:Assertion xmlns:saml=\"urn:oasis:names:tc:SAML:1.0:assertion\""+
						 " AssertionID=\"" + assertionId+ "\""+
						 " IssueInstant=\"" + time + "\""+
						 " Issuer=\"" + issuer + "\""+
						 " MajorVersion=\"1\" MinorVersion=\"1\">"+
                         "<saml:Conditions" +
                         " NotBefore=\"" + time + "\"" +
                         " NotOnOrAfter=\"" + expiryTime +"\"/>"+
						 "<saml:AuthenticationStatement AuthenticationInstant=\"" + time + "\" AuthenticationMethod=\"urn:oasis:names:tc:SAML:1.0:am:password\">"+
						 "<saml:Subject><saml:NameIdentifier>" +userId + "</saml:NameIdentifier>"+
						 "<saml:SubjectConfirmation><saml:ConfirmationMethod>urn:oasis:names:tc:SAML:1.0:cm:bearer</saml:ConfirmationMethod></saml:SubjectConfirmation></saml:Subject>"+
						 "</saml:AuthenticationStatement></saml:Assertion>";
            String samlResposneXml = "<samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:1.0:protocol\" MajorVersion=\"1\" MinorVersion=\"1\" ResponseID=\"" + generateId() +
                                    "\" IssueInstant=\"" + time +
                                    "\" Recipient=\"" + recepient + "\">" +
                                    "<samlp:Status><samlp:StatusCode Value=\"samlp:Success\"/></samlp:Status>" +
                                    assertionXml +
                                    "</samlp:Response>";
            
            //Load X509 cert with matching private signing key from MY store on LocalMachine
            X509Store store = new X509Store("MY", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, keyName, false);
            if (certs.Count == 0) throw new SAMLAssertionCreationException("No Certificates with name: "+keyName+ " in Local Machine Personal store. Make sure you load saml_dsa.pfx in the correct store as described in readme.txt");
            X509Certificate2 cert = (X509Certificate2)store.Certificates.Find(X509FindType.FindBySubjectName, keyName, false)[0];
            
            //keyInfo.AddClause(New RSAKeyValue(key))
            return signXML(samlResposneXml, cert);          
            
        }


	}
    public class SAMLAssertionCreationException : Exception
    {



        public SAMLAssertionCreationException(String msg)
            : base(msg)
        {
        }


        public SAMLAssertionCreationException(String msg, Exception parentEx)
            : base(msg, parentEx)
        {
        }


    }

}
