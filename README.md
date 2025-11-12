# prueba-amaris-back2
prueba tecnica amaris .net

#unitest
##correr pruebas unitarias general 
dotnet test 
## correr pruebas unitarias filtradas 

dotnet test --filter subscriptions

##comandos para generar 
### reporte xml
dotnet test /Users/arhtur/pruebaTecnicaAmaris/TechnicalTest.Solution.sln \
     --collect:"XPlat Code Coverage" \
     --results-directory /Users/arhtur/pruebaTecnicaAmaris/test/TestResults \
     -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

###instala el generador de reportes HTML

dotnet tool install --global dotnet-reportgenerator-globaltool

### reporte html
   reportgenerator \
     "-reports:/Users/arhtur/pruebaTecnicaAmaris/test/TestResults/**/coverage.cobertura.xml" \
     "-targetdir:/Users/arhtur/pruebaTecnicaAmaris/test/TestResults/CoverageReport" \
     "-reporttypes:Html"