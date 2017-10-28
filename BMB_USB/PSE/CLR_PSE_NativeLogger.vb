Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Global.PSE
    Friend NotInheritable Class CLR_PSE_NativeLogger
        Inherits TextWriter

        Const STDIN As UInt16 = 0
        Const STDOUT As UInt16 = 1
        Const STDERR As UInt16 = 2

        Private Class NativeMethods
            <DllImport("ucrtbase.dll", CallingConvention:=CallingConvention.Cdecl)>
            Public Shared Function __acrt_iob_func(var As UInt16) As IntPtr
            End Function

            <DllImport("ucrtbase.dll", CallingConvention:=CallingConvention.Cdecl)>
            Public Shared Function __stdio_common_vfprintf(_Options As UInt64, _Stream As IntPtr, _Format As Byte(), _Local As IntPtr, _ArgList() As IntPtr) As Int32
            End Function
        End Class

        Dim enc As Encoding = New UTF8Encoding()
        Dim fmtBytes() As Byte
        Dim std As UInt16 = 2

        Sub New(useError As Boolean)
            'Init fixed format
            Dim strBytes(enc.GetByteCount("%s") - 1 + 1) As Byte
            Array.Copy(enc.GetBytes("%s"), strBytes, strBytes.Length - 1)
            fmtBytes = strBytes

            If (useError) Then
                std = 2
            Else
                std = 1
            End If
            'printf will auto-expand it to a \r\n
            NewLine = vbLf
        End Sub

        Public Overrides Sub Write(value As Char)
            'Convert string to bytes of needed encoding
            Dim strBytes(enc.GetByteCount(value) - 1 + 1) As Byte
            Array.Copy(enc.GetBytes(value), strBytes, strBytes.Length - 1)
            Dim strHandle As GCHandle = GCHandle.Alloc(strBytes, GCHandleType.Pinned)

            Try
                'write bytes to stdstream
                NativeMethods.__stdio_common_vfprintf(
                    0,
                    NativeMethods.__acrt_iob_func(std),
                    fmtBytes,
                    IntPtr.Zero,
                    New IntPtr() {strHandle.AddrOfPinnedObject()})
            Finally
                strHandle.Free()
            End Try
        End Sub

        Public Overrides ReadOnly Property Encoding As Encoding
            Get
                Return enc
            End Get
        End Property
    End Class
End Namespace
