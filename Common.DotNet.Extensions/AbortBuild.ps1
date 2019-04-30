
# Abort by sending Ctrl+Break
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")
[System.Windows.Forms.SendKeys]::SendWait("^{BREAK}") 
