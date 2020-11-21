#VARIABLES SPECIFIC TO YOUR ENVIRONMENT
#*****************************************
#ENVIRONMENT SPECIFIC VARIABLES
#regular weblogic server template
domain_template='c:/bea92/weblogic92/common/templates/domains/wls.jar'
example_home='c:/saml_example'
iis_intersite_transfer_url='http://localhost/SAMLdotNet/Login.aspx'

#VARIABLES BELLOW SHOULD NOT REQUIRE CHANGING UNLESS YOU DEVIATE FROM THE HAPPY PATH
admin_user='weblogic'
admin_password='weblogic'
admin_server_name='AdminServer'
admin_server_url='t3://localhost:7001'
domain_name='wls_domain'
protected_jsp='/protected/secret.jsp'
certificate_file_name='saml_dsa.cer'
protected_war_location=example_home+'/protected'
certificate_location=example_home+'/'+certificate_file_name
domain_home=example_home+'/'+domain_name
first_start_log=domain_home+'/first_start.log'
second_start_log=domain_home+'/second_start.log'
#*****************************************
#END OF VARIABLE DEFININTIONS

#configure the domain
print('**** Creating basic domain')
readTemplate(domain_template)
cd('/Security/base_domain/User/weblogic')
cmo.setPassword('weblogic')
cd('/')
cmo.setName(domain_name)
writeDomain(domain_home)
print('**** Domain created')

print('**** Starting server. Logs redirected to ' + first_start_log)
startServer(admin_server_name,domain_name, admin_server_url,admin_user,admin_password,domain_home, 'true', serverLog=first_start_log)
print('**** Configuration stage 1 about to start')
connect( admin_user, admin_password, admin_server_url)
edit()
startEdit()
#create SAML Identity asserter
cd('/SecurityConfiguration/'+domain_name+'/Realms/myrealm')
cmo.createAuthenticationProvider('SAML Identity Asserter','weblogic.security.providers.saml.SAMLIdentityAsserterV2')

#create SAML Authenticator
cmo.createAuthenticationProvider('SAML Authenticator','weblogic.security.providers.saml.SAMLAuthenticator')
cd('/SecurityConfiguration/'+domain_name+'/Realms/myrealm/AuthenticationProviders/SAML Authenticator')
set('ControlFlag','OPTIONAL')

#deploy the application
deploy ('protected', protected_war_location)
activate()
print('**** Configuration stage 1 completed')

#restart required for security provider
print('**** Server shutting down ...')
shutdown()
print('**** Server down')



print('**** Starting server. Logs redirected to ' + second_start_log)
#start server
startServer(admin_server_name,domain_name, admin_server_url,admin_user,admin_password,domain_home, 'true', serverLog=second_start_log)
print('**** Server is up')
print('**** Configuration stage 2 about to start')
connect( admin_user, admin_password, admin_server_url)
edit()
startEdit()

#configure assertion party
#it will go straight into ldap when addAssertingParty is called
print('**** Configuring asserter')
serverConfig()
cd('/SecurityConfiguration/'+domain_name+'/Realms/myrealm/AuthenticationProviders/SAML Identity Asserter')
cmo.registerCertificate('iis_cert',certificate_location)
assertingParty=cmo.newAssertingParty()
assertingParty.setProfile(assertingParty.PROFILE_POST)
assertingParty.setProtocolSigningCertAlias('iis_cert')
assertingParty.setVirtualUserEnabled(true);
assertingParty.setIssuerURI("https://aspsite.com")
assertingParty.setRedirectURIs(jarray.array([String(protected_jsp)], String))
assertingParty.setIntersiteTransferURL(iis_intersite_transfer_url)
assertingParty.setIntersiteTransferParams(jarray.array([String('RPID=rp_00001'), String('APID=ap_00001')], String))
assertingParty.setEnabled(true)
cmo.addAssertingParty(assertingParty);

#time to configure Federation Services
edit()
cd('/Servers/' + admin_server_name + '/FederationServices/AdminServer')
set('DestinationSiteEnabled','true')

#disable https transfer requirement. this exposes you to assertion interception.
set('ACSRequiresSSL','false')
set('AssertionConsumerURIs',jarray.array([String('/samlacs/acs')], String))

#enable security debugging
cd('/Servers/'+admin_server_name+'/ServerDebug/AdminServer')
cmo.createDebugScope('weblogic.security.atn')
cd('/Servers/'+admin_server_name+'/ServerDebug/'+admin_server_name+'/DebugScopes/weblogic.security.atn')
set('Enabled','true')
cd('/Servers/'+admin_server_name+'/ServerDebug/AdminServer')
cmo.createDebugScope('weblogic.security.atz')
cd('/Servers/'+admin_server_name+'/ServerDebug/'+admin_server_name+'/DebugScopes/weblogic.security.atz')
set('Enabled','true')
cd('/Servers/'+admin_server_name+'/ServerDebug/AdminServer')
cmo.createDebugScope('weblogic.security.credmap')
cd('/Servers/'+admin_server_name+'/ServerDebug/'+admin_server_name+'/DebugScopes/weblogic.security.credmap')
set('Enabled','true')
cd('/Servers/'+admin_server_name+'/ServerDebug/AdminServer')
cmo.createDebugScope('weblogic.security.saml')
cd('/Servers/'+admin_server_name+'/ServerDebug/'+admin_server_name+'/DebugScopes/weblogic.security.saml')
set('Enabled','true')

#configure debug levels and redirection to the console
cd('/Servers/'+admin_server_name+'/Log/AdminServer')
set('LogFileSeverity','Debug')
set('StdoutSeverity','Debug')
set('RedirectStdoutToServerLogEnabled','true')


#lastly start the application
activate()
startApplication('protected')
print('**** Configuration stage 2 completed')
#this is for convenience only.
#so you could see console
print('**** Shutting down the server... ')
shutdown()
print('**** Server down')
print('**** Now you can create it from command line')
