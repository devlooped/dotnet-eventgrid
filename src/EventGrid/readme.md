<!-- include ../../readme.md#tool -->
<!-- #tool -->
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
<!-- ../../readme.md#tool -->
<!-- exclude -->`
