# Vidare utveckling av Rules.Engine (VG krav)

## Skapa regel

I Postman, med POST-metoden för endpointen `http://localhost:5105/rules`, har jag skapat regler för **temperatur** och **CO₂** för varje enhet som registrerats via `DeviceRegistry.Api`. För att visualisera databasen och underlätta arbetet med den har jag använt **DBeaver**, vilket gör det möjligt att direkt se all data i databasen.

## Utvärdering

I den första versionen som jag forkat fanns redan en logik som utvärderar det senaste mätvärdet för varje enhet och typ (t.ex. temperatur och co2). Dock visas samma värde varje gång projektet kördes om, om det inte fanns några nyare mätvärden. För att förebygga detta har jag implementerat **watermark-data**, vilket innebär att om de senaste mätvärdena redan har angetts tidigare, noteras detta och sparas i databasen.

Tekniskt sett har jag skapat en ny modelklass, `WatermarkRow`, som innehåller `Id`, `RuleId` och `LastPointTime`. Klassen har lagts till i `RulesDbContext`. `RuleId` används för att identifiera rätt enhet och typ medan `LastPointTime` sparar tidpunkten då det senaste mätvärdet setts.

Vid migreringen för att lägga till den nya tabellen **_*Watermarks*_**, uppstod ett problem med **design time**. Detta berodde på att EF Core försökte hantera `RulesDbContext` som innehåller `WatermarkRow`, men nödvändiga konfigurationsuppgifter saknades under processen.
Efter felsökning hittade och implementerade jag en lösning genom att använda `IDesignTimeDbContextFactory` och skapa `RulesDbContextFactory`.

Watermark-datan används sedan i `ExecuteAsync` när det senaste mätvärdet filtreras genom att jämföra tiderna mellan det nya mätvärdet från `_ingest.Measurements` och den senaste tidpunkten från `_rules.Watermarks` då värdet redan visats. Denna filtrering förhindrar att samma värde visas flera gånger.

## Alert

För att implementera **SignalR** i backend har jag skapat funktionen `PublishAlert` i `Realtime.Hub` som tar emot alert-data som skickas via `hub.InvokeAsync` i `Rules.Engine` och sedan skickar den vidare till frontend.

I `PublishAlert` krävs `TenantSlug` som gruppnamn när datan skickas till frontend. Detta värde fanns inte i den ursprungliga kodbasen jag forkat, så jag lade till en databasanslutning för **_*Tenants*_** i `Rules.Engine` så att `TenantSlug` kan hämtas och skickas med via `_hub.InvokeAsync`.

## Implementering i frontend

I frontend har jag främst arbetat i `sensor-pane.component.ts`. Där har jag lagt till SignalRs `on`-metod med metodnamnet `alertRaised` som motsvarar metoden `PublishAlert` i backendens `Realtime.Hub`. `on`-metoden körs i `ngOnInit` så att den aktiveras direkt när komponenten initierats.

Realtidsdata för alert skickas därefter till funktionen `handleAlertData`. Den enhet som motsvarar realtidsdata hämtas först från listan `devices` (`devices = signal<Device[]>([]);`), som redan är fylld med datan från databasen via Rest API:t. Detta beror på att realtidsdatan inte innehåller enhetsnamn, det vill säga `serial`. Därefter skapas ett nytt alertobjekt, som lagras i `alert` (`alert = signal<Alert | null>(null)`), och funktionen `runToast` anropas.

I `runToast` hanteras alert-informationen som sparats i `alert` med hjälp av npm-paketet **ngx-toastr** som förenklar att skapa toast-aviseringar.
