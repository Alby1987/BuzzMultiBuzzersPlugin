Imports System.Windows.Forms.Control
Namespace Config
    Interface IConfigControl
        Sub AddControlsTo(ByRef ConCollection As ControlCollection)

        Property Top As Integer

        Property Left As Integer
        Property Width As Integer
        ReadOnly Property Bottom As Integer
    End Interface
End Namespace
