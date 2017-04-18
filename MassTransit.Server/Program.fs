open MassTransit.NLogIntegration
open MassTransit
open System
open MassTransit.RabbitMqTransport
open NLog.Config
open NLog.Targets
open NLog.Layouts
open NLog
open MassTransitContracts
open System.Threading.Tasks
open System.Threading

type internal ProcessFileConsumer() =
    static let log = LogManager.GetCurrentClassLogger()
    static let count = ref 0
    
    interface IConsumer<ProcessFileCommand> with
        member __.Consume ctx =
            async {
                let count = Interlocked.Increment count
                if count % 100 = 0 then
                    log.Info(sprintf "%d commands have been received." count)
                let! endpoint = ctx.GetSendEndpoint ctx.Message.ReplayAddress |> Async.AwaitTask
                do! endpoint.Send { CommandId = ctx.Message.CommandId; Result = Ok 1 } |> Async.AwaitTask
            } |> Async.StartAsTask :> Task

[<EntryPoint>]
let main argv =
    LogManager.Configuration <-
        let config = LoggingConfiguration()
        let consoleTarget = new ColoredConsoleTarget(Layout = Layout.op_Implicit "${date:format=HH\:mm\:ss} ${level} ${logger} ${message}")
        config.AddTarget("console", consoleTarget)
        config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget)
        config

    let bus = 
        Bus.Factory.CreateUsingRabbitMq(fun cfg ->
            cfg.UseNLog()
            
            let host =
                cfg.Host(Uri("rabbitmq://localhost"), fun (h: IRabbitMqHostConfigurator) ->
                    h.Username("guest")
                    h.Password("guest"))
            
            cfg.ReceiveEndpoint(host, "process_file_commands", fun cfg ->
                cfg.Consumer<ProcessFileConsumer>()
            )
        )
    
    bus.Start()
    Console.ReadKey() |> ignore
    0