Namespace Global.PSE.CLR_PSE_Callbacks
    'Async callback
    Public Delegate Sub CLR_CyclesCallback(cycles As Integer)
    'PCSX2 Handeler
    Public Delegate Function CLR_IRQHandler() As Integer
End Namespace
