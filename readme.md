![Icon](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/icon-32.png) dotnet-eventgrid
============

[![Version](https://img.shields.io/nuget/v/dotnet-eventgrid.svg?color=royalblue)](https://www.nuget.org/packages/dotnet-eventgrid)
[![Downloads](https://img.shields.io/nuget/dt/dotnet-eventgrid.svg?color=darkmagenta)](https://www.nuget.org/packages/dotnet-eventgrid)
[![License](https://img.shields.io/github/license/devlooped/dotnet-eventgrid.svg?color=blue)](https://github.com/devlooped/dotnet-eventgrid/blob/main/LICENSE)

An Azure Function app with an EventGrid-trigger function that forwards events 
to an Azure SignalR service, and an accompanying `dotnet` global tool to 
connect to it and receive the streaming events in real-time.

![EventGrid tool in action](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/eventgrid.gif)

## Why

I find the [Azure EventGrid Viewer](https://github.com/Azure-Samples/azure-event-grid-viewer) 
quite lacking and stagnating, it's [just a sample after all](https://docs.microsoft.com/en-us/samples/azure-samples/azure-event-grid-viewer/azure-event-grid-viewer/).
Also, I'm much more into [dotnet global tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) 
than web pages, having created a bunch of others like [dotnet-vs](https://github.com/devlooped/dotnet-vs), 
[guit](https://github.com/devlooped/guit), [dotnet-eventgrid](https://github.com/devlooped/dotnet-eventgrid) and 
[dotnet-config](https://github.com/devlooped/dotnet-config) ¯\_(ツ)_/¯

## Install

Now you can install the dotnet tool that connects to your cloud infrastructure:

```
dotnet tool install -g dotnet-eventgrid
```

Update:

```
dotnet tool update -g dotnet-eventgrid
```

<!-- #tool -->

This tool provides a real-time view of Azure EventGrid events, by connecting to an Azure SignalR 
service through an Azure Function app. The function app is triggered by EventGrid events and forwards 
them to the SignalR service, which the tool connects to and receives the events in real-time.

## Usage

```
Usage: eventgrid [url] -[property]* +[property[=minimatch]]*
      +all                    Render all properties
      -property               Exclude a property
      +property[=minimatch]   Include a property, optionally filtering
                              with the given the minimatch expression.
      jq=expression           When rendering event data containing JSON,
                              apply the given JQ expression. Learn more at
                              https://stedolan.github.io/jq/

Examples:
- Include all event properties, for topic ending in 'System'
      eventgrid https://mygrid.com +all +topic=**/System

- Exclude data property and filter for specific event types
      eventgrid https://mygrid.com -data +eventType=Login

- Filter using synthetized path property '{domain}/{topic}/{subject}/{eventType}'
      eventgrid https://mygrid.com +path=MyApp/**/Login

- Filter using synthetized path property for a specific event and user (subject)
      eventgrid https://mygrid.com +path=MyApp/*/1bQUI/Login

- Render sponsorship action, sponsorable login and sponsor login from GH webhook JSON event data:
      eventgrid https://mygrid.com jq="{ action: .action, sponsorable: .sponsorship.sponsorable.login, sponsor: .sponsorship.sponsor.login }"
```

*eventgrid* also supports [.netconfig](https://dotnetconfig.org) for configuring 
arguments:

```gitconfig
[eventgrid]
    url = https://events.mygrid.com

    # filters that events must pass to be rendered
    filter = path=MyApp/**/Login
    filter = eventType=*System*

    # properties to include in the event rendering
    include = EventType
    include = Subject

    # properties to exclude from event rendering
    exclude = data

    # apply jq when rendering JSON data payloads
    jq = "{ action: .action, sponsor: .sponsorship.sponsor.login }"
```

The `url` is the address of your deployed function app, which can optionally 
have an `?key=[access-key]` query string with the same value specified in the 
Function App configuration settings with the name `AccessKey`. If present, it 
will be used as a shared secret to authorize the SignalR stream connection. 
It's passed as the `X-Authorization` custom header and checked by the function 
during SignalR connection negotiation.

Keep in mind that the built-in EventGrid format for `topic` is rather unwieldy: 
`/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/domains/{domainName}/topics/{topicName}`. 
For this reason, we also provide a synthetized `path` property with the much 
simpler format `{domain}/{topic}/{subject}/{eventType}`, which makes filtering 
with the [minimatch](https://github.com/isaacs/minimatch) format much more 
convenient.

<!-- #tool -->

If you already know how to deploy an Azure SignalR service, you can safely 
skip the following section.

## Deploy

The dotnet global tool `eventgrid` connects to a SignalR service that broadcasts events with a 
specific format (basically, just JSON-serialized [EventGridEvent](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models.eventgridevent?view=azure-dotnet) 
objects). In order to receive those, we need to connect an EventGrid subscription (thorugh an 
Azure function) to SignalR. Since the resources, cost and privacy issues involved are non-trivial, 
we don't provide a ready-made service you can just connect your EventGrid events to. 

Instructions to deploy the cloud pieces on your own Azure subscription:

1. The first step to getting your own event grid events routed to the tool is to 
   set up a [Azure SignalR service](https://portal.azure.com/#create/Microsoft.SignalRGalleryPackage) if 
   you don't have one already. There is a [free tier](https://azure.microsoft.com/en-us/pricing/details/signalr-service/) 
   that allows 20 simulaneous connections and up to 20k messages per day.
   Once created, open the Settings > Keys pane and copy the `Connection String`. 
   Make sure you pick `Serverless` for the service mode.

    > ![SignalR Connection String](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/signalr.png)

3. Next comes the [Function App](https://portal.azure.com/#create/Microsoft.FunctionApp). Create 
   an empty one, using .NET Core 3.1. The simplest way to deploy the code to it is to select the 
   `Deployment Center` pane, select `GitHub` for source control (point it to your fork of this repo) 
   and `App Service build service` for the build provider.

    > ![GitHub source control](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/github.png)

    > ![App Service build service](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/kudu.png)

4. Now we need to configure a couple application settings in the function app:
   * `AzureSignalRConnectionString`: set it to the value copied in step 2.
   * Optionally, create an `AccessKey` value with an arbitrary string to use as a shared 
     secret to authorize connections from the client. You will need to append that key to 
     the url passed to the `eventgrid` tool, like `eventgrid https://myfunc.azurewebsites.net/?key=...`

    > ![Function App configuration](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/configuration.png)

5. The final step is to start sending events to the function app just created. 
   Go to all the relevant EventGrid services you have (or [create a new one](https://portal.azure.com/#create/Microsoft.EventGridDomain)) 
   and set up the subscriptions to push as much or as little as you need to visualize 
   on the tool. Keep in mind that the tool can also do filtering on the client side, 
   so that you don't need to constantly update the subscriptions. During development, 
   it can be convenient to just create a single global subscription with no filters 
   and just filter on the client. Beware of the SignalR service limits for the tier 
   you have selected, though.

   You just need to create a new Event Subscription and select the `Azure Function` 
   endpoint type, and point it to the deployed function app from step 3.

    > ![New Event Subscription](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/eventgrid.png)

   The function will be named `publish` once you select the right subscription, 
   resource group and function app

    > ![Subscription Endpoint](https://raw.githubusercontent.com/devlooped/dotnet-eventgrid/main/img/subscription.png)


### Local Development

When running the function app locally, use [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows#set-a-secret) 
to set the `AzureSignalRConnectionString` and (optional) `AccessKey` values.

## Testing events

Pushing test events to EventGrid is quite simple. Provided you have a package 
reference to `Microsoft.Azure.EventGrid`, you can use the following snippet 
of C# (for example in the most excelent [LINQPad](https://www.linqpad.net/) tool, 
or in a simple top-level C# 9 program) to push some events:

```csharp
var domain = "YOUR_EVENT_GRID_DOMAIN_ENDPOINT_HOSTNAME";                // From the Overview pane
var credentials = new TopicCredentials("YOUR_EVENT_GRID_ACCESS_KEY");   // From Access keys pane

var events = new List<EventGridEvent>
{
    new EventGridEvent(
        id: Guid.NewGuid().ToString("n"), 
        subject: "1bQUI", 
        data: JsonConvert.SerializeObject(new { FirstName = "Daniel", LastName = "Cazzulino" }), 
        eventType: "Login", 
        eventTime: DateTime.UtcNow, 
        dataVersion: "1.0", 
        topic: "Devlooped.MyApp"),
    new EventGridEvent(
        id: Guid.NewGuid().ToString("n"), 
        subject: "1XKDw", 
        data: JsonConvert.SerializeObject(new { FirstName = "Pablo", LastName = "Galiano" }), 
        eventType: "LoginFailed", 
        eventTime: DateTime.UtcNow, 
        dataVersion: "1.0", 
        topic: "Devlooped.MyApp"),
    // ...
};

using (var client = new EventGridClient(credentials))
{
    foreach (var e in events)
    {
        await client.PublishEventsAsync(domain, new List<EventGridEvent> { e });
        Thread.Sleep(1000);
    }
}
```

The above was pretty much what we used to create the animated gif at the top.

<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://avatars.githubusercontent.com/u/71888636?v=4&s=39 "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://avatars.githubusercontent.com/u/87181630?v=4&s=39 "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Khamza Davletov](https://avatars.githubusercontent.com/u/13615108?u=11b0038e255cdf9d1940fbb9ae9d1d57115697ab&v=4&s=39 "Khamza Davletov")](https://github.com/khamza85)
[![SandRock](https://avatars.githubusercontent.com/u/321868?u=99e50a714276c43ae820632f1da88cb71632ec97&v=4&s=39 "SandRock")](https://github.com/sandrock)
[![DRIVE.NET, Inc.](https://avatars.githubusercontent.com/u/15047123?v=4&s=39 "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://avatars.githubusercontent.com/u/16598898?u=64416b80caf7092a885f60bb31612270bffc9598&v=4&s=39 "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://avatars.githubusercontent.com/u/127185?u=7f50babfc888675e37feb80851a4e9708f573386&v=4&s=39 "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://avatars.githubusercontent.com/u/67574?u=3991fb983e1c399edf39aebc00a9f9cd425703bd&v=4&s=39 "Kori Francis")](https://github.com/kfrancis)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![mccaffers](https://avatars.githubusercontent.com/u/16667079?u=110034edf51097a5ee82cb6a94ae5483568e3469&v=4&s=39 "mccaffers")](https://github.com/mccaffers)
[![Seika Logiciel](https://avatars.githubusercontent.com/u/2564602?v=4&s=39 "Seika Logiciel")](https://github.com/SeikaLogiciel)
[![Andrew Grant](https://avatars.githubusercontent.com/devlooped-user?s=39 "Andrew Grant")](https://github.com/wizardness)
[![Lars](https://avatars.githubusercontent.com/u/1727124?v=4&s=39 "Lars")](https://github.com/latonz)
[![prime167](https://avatars.githubusercontent.com/u/3722845?v=4&s=39 "prime167")](https://github.com/prime167)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
