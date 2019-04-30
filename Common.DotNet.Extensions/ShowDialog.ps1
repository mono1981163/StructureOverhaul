[CmdletBinding()]
Param(
   [Parameter(Mandatory=$True,Position=1)]
   [string]
   $message,

   [Parameter(Mandatory=$True,Position=2)]
   [string]
   $title
)


[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")
$output= [System.Windows.Forms.MessageBox]::Show($message, $title, [System.Windows.Forms.MessageBoxButtons]::YesNoCancel, [System.Windows.Forms.MessageBoxIcon]::Warning, [System.Windows.Forms.MessageBoxDefaultButton]::Button2)


if ($output -eq "No")
{
   exit 1
}
if ($output -eq "cancel")
{
   [System.Windows.Forms.SendKeys]::SendWait("^{BREAK}") 
}



#if ($output -eq "YES" )
#{
## ..do something
#}
#else
#{
## ..do something else
#} 