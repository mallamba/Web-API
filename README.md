# Web-API
Lime Assignment to build a simple API for suggesting free meeting times


To use the API they are two methods for the HTTP requests:

1. GET => with URI extensin of api/values/meetings/

   The paremeters are passed on in the URI with a '/' in between
   
        /// Format for ID is an in digits
        
        /// Format for EARLIEST, LATEST is text according to: "MM,DD,YYYYxHHqMMqSSxAM"
        
        /// The ',', 'x', 'q' are left as they are.
        
        /// Format for HOURS is text according to: "HH-HH"
        
        /// The HH are bothe for hour digits and '-' is kept as it is.
        
        /// The last part is ofr the IDs which are written with a '-' in between.
        
    Example for GET-URI:
    
    https://localhost:44316/api/values/meetings/60/01,20,2015x08q00q00xAM/01,22,2015x08q00q00xPM/08-17/57646786307395936680161735716561753784-57646786307395936680161735716561753725
    
    
        /// 60 is LENGTH
        
        /// 01,20,2015x08q00q00xAM is for 01/20/2015 08:00:00 AM
        
        /// 01,22,2015x08q00q00xPM is for 01/22/2015 08:00:00 PM
        
        /// 08-17 is for 08-17
        
        /// 57646786307395936680161735716561753784 is first ID
        
        /// 57646786307395936680161735716561753725 is second ID
        
        
        
        
        
2. POST => with URI extension of api/values/meetings/

   The parameters are passed on in the body all in one array lik:
   
   
   ["60","01,20,2015x08q00q00xAM", "01,22,2015x08q00q00xPM", "08-17", "57646786307395936680161735716561753784", "156281747655501356358519480949344976171"]
