Imports System.Runtime.InteropServices
Namespace Global.PSE
    Friend Class CLR_PSE_SudoPtr
        Inherits SafeBuffer

        Sub New(ptr As IntPtr)
            MyBase.New(True)
            SetHandle(ptr)
        End Sub

        Protected Overrides Function ReleaseHandle() As Boolean
            handle = IntPtr.Zero
            Return True
        End Function
    End Class
End Namespace
