## Architecture diagram

``` mermaid
architecture-beta
    group browser(internet)[Browser]
    
    service client(internet)[client] in browser
    
    group api(cloud)[Azure]

    service db(database)[Database] in api
    service webApi(server)[webApi] in api

    group Internet(internet)[Internet]

    service SoMePlatform1(internet)[SoMe Platform 1] in Internet
    service SoMePlatform2(internet)[SoMe Platform 2] in Internet

    webApi:L -- R:client
    db:L -- R:webApi
    webApi:T -- B:SoMePlatform1
    webApi:T -- B:SoMePlatform2


```