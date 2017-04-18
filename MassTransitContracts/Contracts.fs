namespace MassTransitContracts

open System

type Hash =
    | Md5 of byte[]
    | Sha1 of byte[]
    | Sha256 of byte[]

type ProcessFileCommand =
    { CommandId: Guid
      Timestamp: DateTime
      TaskId: int64
      Path: string option
      Hash: Hash
      Size: int64 
      ReplayAddress: Uri }

type ProcessFileResult =
    { CommandId: Guid
      Result: Result<int, exn> }