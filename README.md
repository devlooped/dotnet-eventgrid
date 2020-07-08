![Icon](https://raw.github.com/devlooped/eventgrid/master/icon/32.png) EventGrid Tool
============

An Azure Function app with an EventGrid-trigger function that forwards events 
to an Azure SignalR service, and an accompanying `dotnet` global tool to 
connect to it and receive the streaming events in real-time.


## install

```
dotnet tool install -g dotnet-eventgrid
```

Update:

```
dotnet tool update -g dotnet-eventgrid
```


## usage

```
Usage: eventgrid [function-app-url] -[property]* +[property:minimatch]*
   -property             Don't show property in rendered output.
   +property:minimatch   Filter entries where the specified property
                         matches the given minimatch expression.
```

Where the `function-app-url` is the address of your deployed function app and 
can optionally have an `?key=[access-key]` query string with the same value 
specified in the Function App configuration settings named `AccessKey`, used 
as a shared secret to authorize the SignalR stream connection.

