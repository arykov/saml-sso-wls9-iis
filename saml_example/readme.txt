This was tested with .Net 2 and WLS 9.2.

Files and directories
---------------------
protected - simple web application with protected resources
SAMLCreator - Visual Studio project, containing SAMLCreator.cs
SAMLdotNet - ASP.Net application that contains Login.aspx
readme.txt - this file
saml_dsa.cer - certificate used during communication between WLS and IIS
saml_dsa.pfx - private key from the pair. It does not have any password protection.
setup_script.py - wlst script to create and configure wls domain
LocalMachineKeyStore.msc - managment console configuration to edit keys and certificates in your local machine key store

Instructions
------------
1. WLS configuration
a. Open setup_script.py and edit environment variables if required.
b. Excute setup_script.py using wlst. For example C:\bea92\weblogic92\common\bin\wlst.cmd setup_script.py
Watch for possible errors. 
Script restarts WLS in the background twice. Logs for those restarts can be found in c:\saml_example\wls_domain\first_start.log and c:\saml_example\wls_domain\second_start.log
At the end of script execution WLS will be shutdown.
c. Start newely created domain

2. IIS configuration
a. Install saml_dsa.pfx into Local Machine My key store.
		You can double click on LocalMachineKeyStore.msc and proceed to step 9.
		If this does not work for some reason you will have to follow all the steps
		1) Start <WINDOWS>\system32\mmc.exe
		2) In the menu select File|Add/Remove Snap-in
		3) Click on Add
		4) Select Certificates and click on Add
		5) Select Computer account and click on Next
		6) Click on Finish
		7) Close Add Standalone Snap-in
		8) Click Ok in Add/Remove Snap-in
		9) In the tree on your left expand Certificates\Personal\Certificates
		10) Right click on Certificates and in All tasks select Import
		11) Click on Next
		12) Change file type to "Personal Information Exchange" and select c:\saml_example\saml_dsa.pfx
		13) Click on Next
		14) Don't enter any password and click next
		15) Click on Finish
b. Grant ASPNET permissions to access the private key using the following command:
winhttpcertcfg -g -c LOCAL_MACHINE\My -s "saml_dsa" -a ASPNET 
This utility can be downloaded here: http://www.microsoft.com/downloads/details.aspx?familyid=C42E27AC-3409-40E9-8667-C748E422833F&displaylang=en
c. Configure IIS site to run on port 80
d. Configure SAMLdotNet Virtual Directory to point to c:\saml_example\SAMLdotNet. Run Scripts permission should be granted 


Test the setup by going to http://localhost:7001/protected/secret.jsp

You should see the following:

	SAML assertion successful!
	Your user principal name is weblogic. 


