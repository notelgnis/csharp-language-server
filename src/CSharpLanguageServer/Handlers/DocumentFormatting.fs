namespace CSharpLanguageServer.Handlers

open System

open Microsoft.CodeAnalysis.Formatting
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.Types.LspResult

open CSharpLanguageServer
open CSharpLanguageServer.State
open CSharpLanguageServer.Types

[<RequireQualifiedAccess>]
module DocumentFormatting =
    let private dynamicRegistration (clientCapabilities: ClientCapabilities option) =
        clientCapabilities
        |> Option.bind (fun x -> x.TextDocument)
        |> Option.bind (fun x -> x.Formatting)
        |> Option.bind (fun x -> x.DynamicRegistration)
        |> Option.defaultValue false

    let provider (clientCapabilities: ClientCapabilities option) : bool option =
        match dynamicRegistration clientCapabilities with
        | true -> None
        | false -> Some true

    let registration (clientCapabilities: ClientCapabilities option) : Registration option =
        match dynamicRegistration clientCapabilities with
        | false -> None
        | true ->
            Some
                { Id = Guid.NewGuid().ToString()
                  Method = "textDocument/formatting"
                  RegisterOptions = { DocumentSelector = Some defaultDocumentSelector } |> serialize |> Some }

    let handle (context: ServerRequestContext) (p: DocumentFormattingParams) : AsyncLspResult<TextEdit [] option> = async {
        match context.GetUserDocument p.TextDocument.Uri with
        | None -> return None |> success
        | Some doc ->
            let! ct = Async.CancellationToken
            let options = FormatUtil.getFormattingOptions doc p.Options
            let! newDoc = Formatter.FormatAsync(doc, options, cancellationToken=ct) |> Async.AwaitTask
            let! textEdits = FormatUtil.getChanges newDoc doc
            return textEdits |> Some |> success
    }