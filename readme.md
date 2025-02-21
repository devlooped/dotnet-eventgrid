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
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Torutek](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/torutek-gh.png "Torutek")](https://github.com/torutek-gh)
[![DRIVE.NET, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/drivenet.png "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Keflon.png "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tbolon.png "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/kfrancis.png "Kori Francis")](https://github.com/kfrancis)
[![Toni Wenzel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/twenzel.png "Toni Wenzel")](https://github.com/twenzel)
[![Uno Platform](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/unoplatform.png "Uno Platform")](https://github.com/unoplatform)
[![Dan Siegel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/dansiegel.png "Dan Siegel")](https://github.com/dansiegel)
[![Reuben Swartz](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/rbnswartz.png "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jfoshee.png "Jacob Foshee")](https://github.com/jfoshee)
[![](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Mrxx99.png "")](https://github.com/Mrxx99)
[![Eric Johnson](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eajhnsn1.png "Eric Johnson")](https://github.com/eajhnsn1)
[![Ix Technologies B.V.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/IxTechnologies.png "Ix Technologies B.V.")](https://github.com/IxTechnologies)
[![David JENNI](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/davidjenni.png "David JENNI")](https://github.com/davidjenni)
[![Jonathan ](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Jonathan-Hickey.png "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Charley Wu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/akunzai.png "Charley Wu")](https://github.com/akunzai)
[![Jakob Tikjøb Andersen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jakobt.png "Jakob Tikjøb Andersen")](https://github.com/jakobt)
[![Tino Hager](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tinohager.png "Tino Hager")](https://github.com/tinohager)
[![Ken Bonny](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KenBonny.png "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/SimonCropp.png "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agileworks-eu.png "agileworks-eu")](https://github.com/agileworks-eu)
[![sorahex](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sorahex.png "sorahex")](https://github.com/sorahex)
[![Zheyu Shen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/arsdragonfly.png "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/vezel-dev.png "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/ChilliCream.png "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/4OTC.png "4OTC")](https://github.com/4OTC)
[![Vincent Limo](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/v-limo.png "Vincent Limo")](https://github.com/v-limo)
[![Jordan S. Jones](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jordansjones.png "Jordan S. Jones")](https://github.com/jordansjones)
[![domischell](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/DominicSchell.png "domischell")](https://github.com/DominicSchell)
[![Joseph Kingry](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jkingry.png "Joseph Kingry")](https://github.com/jkingry)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
