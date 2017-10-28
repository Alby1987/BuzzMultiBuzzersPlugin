Imports System.IO

Namespace Global.PSE
    Friend Class CLR_PSE_PluginLog
        'Constants
        Const UNKOWN As Integer = -1
        Const ERRTRAP As Integer = -2
        Const consoleStdLevel As SourceLevels = SourceLevels.Information And Not (SourceLevels.[Error])
        Const consoleErrLevel As SourceLevels = SourceLevels.[Error]
        Const FileLevel As SourceLevels = SourceLevels.Critical

        'current path to detech if log path has changed
        Shared currentLogPath As String = ""
        'all then loggers
        Shared sources As Dictionary(Of Integer, TraceSource) = Nothing
        Shared fileAll As TraceListener
        'both out and error go to same file
        Shared stdOut As TraceListener
        Shared stdErr As TraceListener

        'Enable AutoFlush
        Shared Sub New()
            Trace.AutoFlush = True
        End Sub

        'Set filter of Source
        Public Shared Sub SetSourceLogLevel(eLevel As SourceLevels, logSource As Integer)
            If sources.ContainsKey(logSource) Then
                sources(logSource).Switch.Level = eLevel
            Else
                Throw New KeyNotFoundException()
            End If
        End Sub
        Public Shared Sub SetSourceUseStdOut(use As Boolean, logSource As Integer)
            If sources.ContainsKey(logSource) Then
                If Not use Then
                    sources(logSource).Listeners.Remove(stdOut)
                ElseIf Not sources(logSource).Listeners.Contains(stdOut) Then
                    sources(logSource).Listeners.Add(stdOut)
                End If
            Else
                Throw New KeyNotFoundException()
            End If
        End Sub
        'Change filter of Listerners (effects all connected sources)
        Public Shared Sub SetStdOutLevel(eLevel As SourceLevels)
            stdOut.Filter = New EventTypeFilter(eLevel)
        End Sub
        Public Shared Sub SetStdErrLevel(eLevel As SourceLevels)
            stdErr.Filter = New EventTypeFilter(eLevel)
        End Sub
        Public Shared Sub SetFileLevel(eLevel As SourceLevels)
            If Not IsNothing(fileAll) Then
                fileAll.Filter = New EventTypeFilter(eLevel)
            End If
        End Sub

        'Add Source to sources, only used in Open()
        Private Shared Sub AddSource(id As Integer, name As String, prefix As String)
            Dim newSource As New TraceSource(prefix & ":" & name)
            newSource.Switch = New SourceSwitch(prefix & ":" & name & ".SS")
            newSource.Switch.Level = SourceLevels.Information
            newSource.Listeners.Remove("Default")
            newSource.Listeners.Add(fileAll)
            newSource.Listeners.Add(stdErr)

            sources.Add(id, newSource)
        End Sub
        Public Shared Sub Open(logFolderPath As String, logFileName As String, prefix As String, sourceIDs As Dictionary(Of UShort, String))
            logFolderPath = logFolderPath.TrimEnd(Path.DirectorySeparatorChar)
            logFolderPath = logFolderPath.TrimEnd(Path.AltDirectorySeparatorChar)
            If sourceIDs Is Nothing Then
                Throw New NullReferenceException()
            End If
            If sources Is Nothing OrElse (currentLogPath <> Convert.ToString(logFolderPath & Convert.ToString("\")) & logFileName) Then
                Close()

                If File.Exists(Convert.ToString(logFolderPath & Convert.ToString("\")) & logFileName) Then
                    Try
                        File.Delete(Convert.ToString(logFolderPath & Convert.ToString("\")) & logFileName)
                    Catch
                    End Try
                End If

                'Console Normal
                stdOut = New TextWriterTraceListener(New CLR_PSE_NativeLogger(False))
                stdOut.Filter = New EventTypeFilter(consoleStdLevel)
                'information
                stdOut.Name = "StdOut"
                'Console Error
                stdErr = New TextWriterTraceListener(New CLR_PSE_NativeLogger(True))
                stdErr.Filter = New EventTypeFilter(consoleErrLevel)
                stdErr.Name = "StdErr"

                currentLogPath = Convert.ToString(logFolderPath & Convert.ToString("\")) & logFileName
                'Text File
                Try
                    fileAll = New TextWriterTraceListener(currentLogPath)
                    fileAll.Filter = New EventTypeFilter(FileLevel)
                    fileAll.Name = "File"
                Catch e As Exception
                    stdErr.WriteLine("Failed to Open Log File :" + e.ToString())
                End Try

                'Create sources
                sources = New Dictionary(Of Integer, TraceSource)()
                'Defualt Sources
                AddSource(UNKOWN, "UnkownSource", prefix)
                SetSourceLogLevel(SourceLevels.All, UNKOWN)
                SetSourceUseStdOut(True, UNKOWN)
                AddSource(ERRTRAP, "ErrorTrapper", prefix)
                SetSourceUseStdOut(True, ERRTRAP)

                For Each sourceID As KeyValuePair(Of UShort, String) In sourceIDs
                    AddSource(sourceID.Key, sourceID.Value, prefix)
                Next
            End If
        End Sub
        Public Shared Sub Close()
            If sources Is Nothing Then
                Return
            End If
            For Each source As KeyValuePair(Of Integer, TraceSource) In sources
                'will close all listerners
                source.Value.Close()
            Next
            sources.Clear()
            sources = Nothing
        End Sub

        Public Shared Sub WriteLine(eType As TraceEventType, logSource As Integer, str As String)
            If sources Is Nothing Then
                Return
            End If
            If sources.ContainsKey(logSource) Then
                sources(logSource).TraceEvent(eType, logSource, str)
            Else
                sources(UNKOWN).TraceEvent(eType, logSource, str)
            End If
        End Sub

        Public Shared Sub MsgBoxError(e As Exception)
            Console.[Error].WriteLine(e.Message + Environment.NewLine + e.StackTrace)
            System.Windows.Forms.MessageBox.Show("Encounted Exception! : " + e.Message + Environment.NewLine + e.StackTrace)
            Try
                'System.IO.File.WriteAllLines(logPath + "\\" + libraryName + " ERR.txt", new string[] { e.Message + Environment.NewLine + e.StackTrace });
                If sources IsNot Nothing Then
                    WriteLine(TraceEventType.Critical, ERRTRAP, e.Message + Environment.NewLine + e.StackTrace)
                Else
                    Throw New Exception("Error Before Log Open")
                End If
            Catch
                Console.[Error].WriteLine("Error while writing ErrorLog")
            End Try
        End Sub
    End Class
End Namespace
