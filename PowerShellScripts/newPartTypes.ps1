param (
  $path = ""
)

#Open localization file
$locPath = $path + "parts.loc"
$streamReader = [System.IO.File]::OpenText($locPath)

#Array to hold distinct translated lines
$locSet = New-Object System.Collections.Generic.HashSet[String]

#Read loc file and add translated lines to array
while ($streamReader.Peek() -ge 0)
{
  $line = $streamReader.ReadLine()
  #translated line is in second tab
  $locSet.Add($line.Split("`t")[1]) | Out-Null
}
$streamReader.Close()

#Open price
$pricePath = $path + "price.tsv"
$streamReader = [System.IO.File]::OpenText($pricePath)

#Array to hold new part types from price
$newPartTypes = New-Object System.Collections.Generic.HashSet[String]

#Read price file, find new type and add part types to array
while ($streamReader.Peek() -ge 0)
{
  $line = $streamReader.ReadLine()
  #Part type is in 5th tab
  $partType = $line.Split("`t")[4]
  if (-not $locSet.Contains($partType))
  {
    $newPartTypes.Add($partType) | Out-Null
  }
}
$streamReader.Close()

#Make ouput
'New types from price:'
if ($newPartTypes.Count -eq 0)
{
  'No new types'
}
else
{
  $newPartTypes
}