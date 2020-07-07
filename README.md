![Icon](https://raw.github.com/kzu/wsrelay/master/icon/32.png) EventStream
============

An Azure Function app that with an EventGrid-trigger function that forwards 
events to an Azure SignalR service, and an accompanying `dotnet` global tool 
to connect to the function and receive the streaming events in real-time.

## install

```
dotnet tool update -g dotnet-eventstream --no-cache --add-source https://pkg.kzu.io/index.json
```

## usage

```
eventstream [function-app-url]?accessKey=[access-key]
```

Where the `function-app-url` is the address of your deployed function app and 
`access-key` equals a Function App configuration setting named `AccessKey` 
containing an arbitrary string used as a shared secret to authorize the event 
stream connection.
