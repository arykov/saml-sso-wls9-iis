<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Login" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
</head>
<body>
    
        <% 
            /*
             *  Read request parameters 
             */
            String target = Request.Params.Get("TARGET");            
            String apid = Request.Params.Get("APID");
            String rpid = Request.Params.Get("RPID");
            String redirectURL = null;
            //this should be configurable
            if (rpid != null && rpid.Equals("rp_00001"))
            {
                redirectURL = "http://localhost:7001/samlacs/acs";
            }
            if(rpid == null || redirectURL == null)
            {
                Response.Write("Unknown Resource Provider ID(RPID): " + rpid);
            }else if (apid == null){
                Response.Write("Assertion Party ID(APID) cannot be empty.");

            }
            else if (target == null)
            {
                Response.Write("Target (TARGET) cannot be empty.");
            }
            /*
             * We were passed everything we need to create an assertion
             */
            else
            {                
                saml.SAMLAssertionCreator assertionCreator = new saml.SAMLAssertionCreator();
                /*
                 * For purposes of this example no authentication is performed at any level.
                 * In real scenario this HAS TO BE FIXED. 
                 * 
                 */

                try
                {

                    String assertionResponse = assertionCreator.createAssertion("weblogic", "https://aspsite.com", redirectURL, "saml_dsa");
                    
                    %>     
                     
                          <form name="GoToWLS" action="<%=redirectURL%>" method="post">
                            <input type="hidden" name="TARGET" value="<%=target%>" />
                            <input type="hidden" name="SAMLResponse" value="<%=Convert.ToBase64String(Encoding.UTF8.GetBytes(assertionResponse))%>"/>
                            <input type="hidden" name="APID" value="<%=apid%>" />
                            <!--input type="submit" value="Submit"/-->                        
                        </form>
                        <SCRIPT language="JavaScript">
	                        document.GoToWLS.submit()
                        </SCRIPT>
                     <% 
                }
                catch (saml.SAMLAssertionCreationException sace)
                {
                    //end user should not see this internal message
                    Response.Write(sace.Message);
                }
            }
       %>                
</body>
</html>
