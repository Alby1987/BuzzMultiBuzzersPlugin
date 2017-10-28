Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices

Class Utils
    Private Class NativeMethods
        <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
        Public Shared Function GetForegroundWindow() As IntPtr
        End Function
        <SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist", Justification:="Used Only in x86")>
        <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
        Public Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer 'Will always return 4bits
        End Function '32bit
        <SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist", Justification:="Used Only in x64")>
        <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
        Public Shared Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
        End Function '64bit
        <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
        Public Shared Function GetParent(hWnd As IntPtr) As IntPtr
        End Function
        <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
        Public Shared Function IsWindow(hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function
    End Class
    Private Const GWL_STYLE As Integer = -16
    Private Const WS_CHILD As Long = &H40000000L

    Public Shared Sub memcpy(ByRef target As Byte(), ByVal targetstartindex As Integer, ByRef source As Byte(), ByVal sourcestartindex As Integer, ByVal num As Integer)
        For x As Integer = 0 To num - 1
            target(targetstartindex + x) = source(sourcestartindex + x)
        Next
    End Sub

    Public Shared Function GetForegroundWindow() As IntPtr
        Return NativeMethods.GetForegroundWindow()
    End Function

    Public Shared Function GetTopParent(hWnd As IntPtr) As IntPtr
        Dim hWndTop As IntPtr = hWnd
        While (GetWindowLongAuto(hWndTop, GWL_STYLE).ToInt64 And WS_CHILD) <> 0
            hWndTop = NativeMethods.GetParent(hWndTop)
        End While
        Return hWndTop
    End Function
    Private Shared Function GetWindowLongAuto(hWnd As IntPtr, nIndex As Integer) As IntPtr
        If (IntPtr.Size = 4) Then
            Return New IntPtr(NativeMethods.GetWindowLong(hWnd, nIndex))
        Else
            Return NativeMethods.GetWindowLongPtr(hWnd, nIndex)
        End If
    End Function
    Public Shared Function IsAWindow(hWnd As IntPtr) As Boolean
        Return NativeMethods.IsWindow(hWnd)
    End Function
    ''Not safe
    'Public Function IsBadPtrPtr(lp As IntPtr) As Boolean
    '    Return IsBadReadPtr(lp, IntPtr.Size)
    'End Function
End Class
