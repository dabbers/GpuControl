var   tls = require('tls')
    , fs = require('fs')
    , aServers = {"MitbHKfBH7Q16R9S":{"state":"OFFLINE"},
            "YitbHKfBH7Q16R9S":{"name":"empty","state":"OFFLINE"}}
    , client = undefined
    , aAuth = ["yourmother","ohno"];




var options = {
  key: fs.readFileSync('/Users/dab/node-key.pem'),
  cert: fs.readFileSync('/Users/dab/node-cert.pem')
};

tls.createServer(options, function (s) {
  console.log( "Client connected!\n" );
  s.setEncoding('utf8');
  s.on( 'error', function( msg ) { console.log( msg ); } );
  s.on( 'close', function() {
        for ( var mach in aServers )
        {
          if ( aServers[mach].state == s )
          {
            aServers[mach].state = "OFFLINE"
          }
        }
       } );
  s.on( 'data', function( d )
  {
    console.log( d );
    var a = d.split( " " );
    if ( a[0] == "hi" )
    {
      if ( a[1] in aServers )
      {
        console.log("PASS");
        aServers[a[1]].state = s;
        aServers[a[1]].name = a[2];
        console.log( aServers[a[1]].name);
        s.write( "hello 1 " + aServers[a[1]].name + "\n" );
        
        if ( client != undefined )
          s.write( "start\n");
      }
      else
      {
        s.write( "hello 0\n" );
      }
    }
    else if ( a[0] == "fancontrol" )
    {
      if ( a[1] == "Permission" )
      {
        if ( client == undefined )
          return;
        var sServerName = "unknown";
        
        for ( var mach in aServers )
        {
          if ( aServers[mach].state == s )
          {
            sServerName = aServers[mach].name;
          }
        }
        
        client.emit( "denied", {"name":sServerName});
      }
    }
    else
    {
      if ( client != undefined )
      {
         client.emit('news', d );
      }
    }
  });
}).listen(8000);



var app = require('http').createServer(handler)
  , io = require('socket.io').listen(app)
  , fs = require('fs')

app.listen(8080);

function handler (req, res) {

  fs.readFile(__dirname + '/index.html',
  function (err, data) {
    if (err) {
      res.writeHead(500);
      return res.end('Error loading index.html');
    }

    res.writeHead(200);
    res.end(data);
  });
}

io.sockets.on('connection', function (socket) {

  socket.on( 'login', function( data )
       {
        console.log( data );
        for( var i in aAuth )
        {
          if ( data.pw == aAuth[i] )
          {
            client = socket;
            socket.emit( 'auth', {result:"success"} );
            console.log( client );
            return;
          }
          
        }
        
            socket.emit( 'auth', {result:"fail"} );
       });
  socket.on('init', function (data) {

    if ( client == undefined ) return;
    
    for ( var mach in aServers )
    {
      if ( aServers[mach].state != "OFFLINE" && aServers[mach].state != undefined )
      {
        aServers[mach].state.write( "start\n" );
      }
    }
  });
  socket.on( 'fan', function( data ) {
    if ( client == undefined ) return;
    
    for( var mach in aServers )
    {

      if ( aServers[mach].name == data.machine && aServers[mach].state != "OFFLINE")
      {
        aServers[mach].state.write( "fancontrol " + data.gpu + " " + data.value + "\n" );
        return;
      }
    }
  });
  socket.on('disconnect', function (data) {
    if ( client == undefined ) return;
    
    for ( var mach in aServers )
    {
      if ( aServers[mach].state != "OFFLINE" && aServers[mach].state != undefined )
      {
        aServers[mach].state.write( "stop\n" );
      }
    }
  });
});