module CSharpLanguageServer.Program

open System
open System.IO
open System.Threading
open System.Linq
open LSP
open LSP.Types
open LSP.Log

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.MSBuild
open Microsoft.CodeAnalysis.FindSymbols
open Microsoft.CodeAnalysis.Text;
open FSharp.Control.Tasks.V2
open Microsoft.Build.Locator

type Server(client: ILanguageClient) =
    let mutable workspace: Workspace option = None

    let logMessage message = client.ShowMessage { ``type`` = MessageType.Log ;
                                                   message = "cs-lsp-server: " + message } |> ignore

    let mutable deferredInitialize = async {
        let solutionPath = "/Users/bob/src/omnisharp/test/test.sln"
        logMessage ("in deferredInitialize, loading solution: " + solutionPath)

        let msbuildWorkspace = MSBuildWorkspace.Create()
        let! testSolution = msbuildWorkspace.OpenSolutionAsync(solutionPath) |> Async.AwaitTask

        logMessage "in deferredInitialize, ok solution loaded"

        workspace <- Some(msbuildWorkspace :> Workspace)
        ()
    }

    let currentSolution () = match workspace with
                             | Some ws -> Some ws.CurrentSolution
                             | _ -> None

    let todo() = raise (Exception "TODO")

    interface ILanguageServer with
        member _.Initialize(_: InitializeParams) =
            async {
                return {
                   capabilities =
                       { defaultServerCapabilities with
                             hoverProvider = true
                             completionProvider = None
                             signatureHelpProvider = None
                             documentSymbolProvider = false
                             codeLensProvider = None
                             workspaceSymbolProvider = false
                             definitionProvider = false
                             referencesProvider = false
                             renameProvider = false
                             textDocumentSync = {
                                defaultTextDocumentSyncOptions with
                                      openClose = false
                                      save = Some({ includeText = false })
                                      change = TextDocumentSyncKind.Full
                             }
                         }
                   }
                }

        member __.Initialized(): Async<unit> =
            deferredInitialize

        member __.Shutdown(): Async<unit> = todo()
        member __.DidChangeConfiguration(_: DidChangeConfigurationParams): Async<unit> = todo()
        member __.DidOpenTextDocument(_: DidOpenTextDocumentParams): Async<unit> =
            async {
                return ()
            }

        member __.DidChangeTextDocument(change: DidChangeTextDocumentParams): Async<unit> =
            task {
                match currentSolution () with
                | Some solution ->
                    let project = solution.Projects.Single(fun p -> p.Name = "test")
                    let! compilation = project.GetCompilationAsync()
                    let doc = project.Documents.First()

                    let fullText = SourceText.From(change.contentChanges.[0].text)

                    let updatedDoc = doc.WithText(fullText)
                    let updatedSolution = updatedDoc.Project.Solution;

                    let applySucceeded = workspace.Value.TryApplyChanges(updatedSolution)

                    if not applySucceeded then
                        logMessage "workspace.TryApplyChanges has failed!"
                    else
                        logMessage "workspace.TryApplyChanges has succeeded!"

                | _ -> ()

                return ()
            } |> Async.AwaitTask

        member __.WillSaveTextDocument(_: WillSaveTextDocumentParams): Async<unit> = todo()
        member __.WillSaveWaitUntilTextDocument(_: WillSaveTextDocumentParams): Async<TextEdit list> = todo()
        member __.DidSaveTextDocument(_: DidSaveTextDocumentParams): Async<unit> =
            async {
                return ()
            }

        member __.DidCloseTextDocument(_: DidCloseTextDocumentParams): Async<unit> =
            async {
                return ()
            }

        member __.DidChangeWatchedFiles(_: DidChangeWatchedFilesParams): Async<unit> = todo()
        member __.Completion(_: TextDocumentPositionParams): Async<CompletionList option> = todo()

        member __.Hover(hoverPos: TextDocumentPositionParams): Async<Hover option> =

            let resolveHover (solution: Solution) = task {
                let project = solution.Projects.Single(fun p -> p.Name = "test")
                let! compilation = project.GetCompilationAsync()
                let firstDoc = project.Documents.First()
                let! sourceText = firstDoc.GetTextAsync()
                let! semanticModel = firstDoc.GetSemanticModelAsync()

                let position = sourceText.Lines.GetPosition(LinePosition(hoverPos.position.line, hoverPos.position.character))

                let! symbol = SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace.Value)

                let hoverText = match symbol with
                                | null -> //logMessage "no symbol at this point"
                                          ""
                                | sym -> //logMessage ("have symbol " + sym.ToString() + " at this point!")
                                         symbol.ToString() + "\n" +  symbol.GetDocumentationCommentXml()

                return [ HighlightedString(hoverText, "fsharp") ;
                         PlainString("hey") ]
            }

            async {
                let! contents = match currentSolution () with
                                | Some solution -> resolveHover solution |> Async.AwaitTask
                                | None -> async {
                                             return [ HighlightedString("none", "fsharp") ;
                                                      PlainString("none") ]
                                         }
                return Some({ contents=contents; range=None })
            }

        member __.ResolveCompletionItem(_: CompletionItem): Async<CompletionItem> = todo()
        member __.SignatureHelp(_: TextDocumentPositionParams): Async<SignatureHelp option> = todo()
        member __.GotoDefinition(_: TextDocumentPositionParams): Async<LSP.Types.Location list> = todo()
        member __.FindReferences(_: ReferenceParams): Async<LSP.Types.Location list> = todo()
        member __.DocumentHighlight(_: TextDocumentPositionParams): Async<DocumentHighlight list> = todo()
        member __.DocumentSymbols(_: DocumentSymbolParams): Async<SymbolInformation list> = todo()
        member __.WorkspaceSymbols(_: WorkspaceSymbolParams): Async<SymbolInformation list> = todo()
        member __.CodeActions(_: CodeActionParams): Async<Command list> = todo()
        member __.CodeLens(_: CodeLensParams): Async<List<CodeLens>> = todo()
        member __.ResolveCodeLens(_: CodeLens): Async<CodeLens> = todo()
        member __.DocumentLink(_: DocumentLinkParams): Async<DocumentLink list> = todo()
        member __.ResolveDocumentLink(_: DocumentLink): Async<DocumentLink> = todo()
        member __.DocumentFormatting(_: DocumentFormattingParams): Async<TextEdit list> = todo()
        member __.DocumentRangeFormatting(_: DocumentRangeFormattingParams): Async<TextEdit list> = todo()
        member __.DocumentOnTypeFormatting(_: DocumentOnTypeFormattingParams): Async<TextEdit list> = todo()
        member __.Rename(_: RenameParams): Async<WorkspaceEdit> = todo()
        member __.ExecuteCommand(_: ExecuteCommandParams): Async<unit> = todo()
        member __.DidChangeWorkspaceFolders(_: DidChangeWorkspaceFoldersParams): Async<unit> = todo()

[<EntryPoint>]
let main(_: string array): int =
    MSBuildLocator.RegisterDefaults() |> ignore

    let read = new BinaryReader(Console.OpenStandardInput())
    let write = new BinaryWriter(Console.OpenStandardOutput())
    let serverFactory(client) = Server(client) :> ILanguageServer
    dprintfn "Listening on stdin"
    try
        LanguageServer.connect(serverFactory, read, write)
        0 // return an integer exit code
    with e ->
        dprintfn "Exception in language server %O" e
        1
