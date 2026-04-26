<?
Include "lib/syntax/spo.php";

function Trim_($text){ global $g;
 $pos = 0;
 $len = StrLen($text);
 while(1){
  if($pos >= StrLen($text))
   return $text;
  $pos = StrPos($text, "\n", $pos+1);
  if($pos && !Trim(SubStr($text, 0, $pos)))
   $text = SubStr($text, $pos+1);
  else
   return $text;
 }
}

function SynRest($in){ global $g;
 $out =
 str_replace("<font style='background-color:#FF3333'>[</font>\\", "[",
 str_replace("\\<font style='background-color:#FF3333'>]</font>", "]",
 str_replace("[", "<font style='background-color:#FF3333'>[</font>",
 str_replace("]", "<font style='background-color:#FF3333'>]</font>",
 str_replace("[<font style='background-color:#FF3333'>|</font>]", "|",
 str_replace("|", "<font style='background-color:#FF3333'>|</font>",
 str_replace('[!', '<font color="#ff0000">',
 str_replace('!]', '</font>',
 str_replace('[(',  '<small>',
 str_replace(')]', '</small>',
 str_replace("['",  '<sup>',
 str_replace("']", '</sup>',
 str_replace("['",  '<sup>',
 str_replace("']", '</sup>',
 str_replace('[,',  '<sub>',
 str_replace(',]', '</sub>',
 str_replace('[/',  '<i>',
 str_replace('/]', '</i>',
 str_replace('[*',  '<b>',
 str_replace('*]', '</b>',
 str_replace('[_',  '<u>',
 str_replace('_]', '</u>',
 str_replace('[-',  '<s>',
 str_replace('-]', '</s>',
 str_replace('[---]', '<img src="img/r_w.gif" width="100%" height="10">',
 str_replace('<', '&lt;',
 str_replace('>', '&gt;',
 str_replace('[>', '&#187;',
 str_replace('<]', '&#171;',
 str_replace('["', '&#8222;',
 str_replace('"]', '&#8221;',
 $in)))))))))))))))))))))))))))))));
 
 $out = preg_replace('"(((ftp|http|https){1}://)[-a-zA-Z0-9@:%_\+.~#?&//=]+)"i' , '<a href="\1" target="_blank">\\1</a>'          , $out);
 $out = preg_replace('"( |^)(www.[-a-zA-Z0-9@:%_\+.~#?&//=]+)"i'                , '\\1<a href="http://\2" target="_blank">\\2</a>', $out);
 $out = preg_replace('"(( |^)[_\.0-9a-z-]+@([0-9a-z][0-9a-z-]+\.)+[a-z]{2,3})"i', '<a href="mailto:\1">\\1</a>'                   , $out);
 return $out; //preg_replace('((((http|ftp)://(^ )*)|www|WWW)\.(^ )*)', '<a href="\\1">\\1</a>', $out);
}

//==============================================================================
function SynAbsatz($in){ global $g, $url;
 $in = '   '.$in;
 $pos  = 0;
 $pos2 = 1;
 while(1){
  if(StrPos($in, '[<', $pos2) > StrPos($in, '[?', $pos2)){
   $type_ = '<>';
   $pos = StrPos($in, '[<', $pos2);
   $pos1 = StrPos($in, '>]', $pos);
   if(!$pos or !$pos1){
    $type = '??';
    $pos = StrPos($in, '[?', $pos2);
    $pos1 = StrPos($in, '?]', $pos);
   }
  }
  else{
   $type_ = '??';
   $pos = StrPos($in, '[?', $pos2);
   $pos1 = StrPos($in, '?]', $pos);
  }

  if(!$pos or !$pos1)
   break;

  $synonym = '';
  $out .= SynRest(SubStr($in, $pos2+2, $pos-$pos2-2));
  $pos2 = $pos1;
  $link = SubStr($in, $pos+2, $pos2-$pos-2);
  if($pos3 = StrPos($link, '|')){
   $synonym = SubStr($link, $pos3+1);
   $link = SubStr($link, 0, $pos3);
  }
   if(!$synonym){
    $synonym = $link;
   }
   if($type_ == '<>')
    $out .= "<a href='". $ini['Setup']['url'].urlencode($link).".html'>$synonym</a>";
    //  $out .= "<a href='".$ini['Setup']['url']."?$link'>$synonym</a>";
   else
    $out .= "<a href='".$ini['Setup']['url']."board.php?titel=$link'>$synonym</a>";
 }
 return $out.SynRest(SubStr($in, $pos2+2));
}

//==============================================================================
function SynText($in){ global $g;
 $in   = Split("\n", Trim_($in));
 $size = SizeOf($in);
 $absatz = 0;
 
 while($absatz < $size){
  if    (SubStr(Trim($in[$absatz]), 0, 2) == "o ")
   $out .= "<ul type=\"circle\"><li>".SynAbsatz(SubStr(Trim($in[$absatz]), 2))."</li></ul>\n";
  elseif(SubStr(Trim($in[$absatz]), 0, 2) == "# ")
   $out .= "<ul type=\"square\"><li>".SynAbsatz(SubStr(Trim($in[$absatz]), 2))."</li></ul>\n";
  elseif(SubStr(Trim($in[$absatz]), 0, 2) == "* ")
   $out .= "<ul type=\"disc\"><li>".SynAbsatz(SubStr(Trim($in[$absatz]), 2))."</li></ul>\n";
  elseif(SubStr(Trim($in[$absatz]), 0, 2) == "> ")
   $out .= "<ul style=\"list-style-image:url(img/dreieck.gif)\"><li>".SynAbsatz(SubStr(Trim($in[$absatz]), 2))."</li></ul>\n";
  elseif(SubStr(Trim($in[$absatz]), 0, 2) == "- ")
   $out .= "<ul style=\"list-style-image:url(img/anstrich.gif)\"><li>".SynAbsatz(SubStr(Trim($in[$absatz]), 2))."</li></ul>\n";
  elseif(SubStr($in[$absatz], 0, 2) == "  ")
   $out .= "<ul>".SynAbsatz(SubStr($in[$absatz], 2))."</ul>\n";
  else
   $out .= Trim(SynAbsatz($in[$absatz]))."<br>\n";
  $absatz++;
 }

 return $out;
}

//==============================================================================
function SynTabelle($in){ global $g;
 $bg = 0;
 $pos1 = $pos = 0;
 $out  = "
<table border='1' bordercolor='#000000' rules='all' cellpadding='5'>
 <tr bgcolor='#ddddff'>
  <td>";

 while(1){
  $pos2 = StrPos($in, '|', $pos);

  if(!$pos2){
   $out .= SynText(RTrim(SubStr($in, $pos1)))."
  </td>
 </tr>
</table>";
   return $out;
  }

  if(!$nl){
   $code = SubStr($in, $pos2, 2);
   if($code == '||')
    $pos += 2;
   elseif($code == '|]'){
    $nl = 1;
    $out .= SynText(RTrim(SubStr($in, $pos1, $pos2-$pos1)))."
  </td>
 </tr>";
    $pos1 = $pos = $pos2+2;
   }
   else{
    $out .= SynText(RTrim(SubStr($in, $pos1, $pos2-$pos1)))."
  </td>
  <td>";
    $pos1 = $pos = $pos2+1;
   }
  }
  else{
   if(SubStr($in, $pos2-1, 2) == '[|'){
    $nl  = 0;
    if($bg)
     $out .= "
 <tr bgcolor='#ddddff'>
  <td>";
    else
     $out .= "
 <tr bgcolor='#ffffdd'>
  <td>";
    $bg = 1-$bg;
    $pos1 = $pos = $pos2+1;
   }
  }
 }
}

//==============================================================================
function SynObjekt($in){ global $g;
 if(SubStr($in, 0, 1) == "\n")
  $in = SubStr($in, 1);
 elseif(substr($in, 0, 4) == "spo\n")
 	$in = syntax_spo(substr($in, 4));

 $in = Str_Replace("\n", '<br>', Str_Replace('\<', '&lt;', Str_Replace('\>', '&gt;', Str_Replace('\_', ' ', Str_Replace(' ', '&nbsp;', $in)))));

 return "
<table bgcolor='#ffffdd' width='100%' border='1' bordercolor='#000000' rules='all' cellpadding='5'>
 <tr>
  <td>
   <nobr>
    <code>
$in
    </code>
   </nobr>
  </td>
 </tr>
</table>";
}

//==============================================================================
function SynKapitel($in){ global $g;
 if(!SubStr($in, 0, StrPos($in, "\n")))
  $in = SubStr($in, StrPos($in, "\n")+1);

 $pos = 0;
 while(1){
  $pos1 = StrPos(' '.$in, '[|', $pos);
  while(substr(' '.$in, $pos1, 3) == '[|]'){
   $pos1 = StrPos(' '.$in, '[|', $pos1+1);
   if(!$pos1)
    break;
  }
  
  $pos2 = StrPos(' '.$in, '[.', $pos);
  if($pos1 && $pos2){
   if($pos1 < $pos2)
    $pos2 = 0;
   else
    $pos1 = 0;
  }

  if($pos1 && ($pos3 = StrPos($in, '|]', $pos1))){  // TABELLE
   $out .= SynText(Trim_(SubStr($in, $pos, $pos1-$pos-1)));
   while(1){
    if(!($pos = StrPos($in, '[|', $pos3)))
     break;
    if(Trim(SubStr($in, $pos3+2, $pos-$pos3-2)))
     break;
    $pos4 = StrPos($in, "\n", $pos3);
    $pos5 = StrPos($in, "\n", $pos4+1);
    if($pos4 && $pos5  && ($pos5 < $pos))
     break;
    if(!($pos = StrPos($in, '|]', $pos)))
     break;
    $pos3 = $pos;
   }
   $out .= SynTabelle(SubStr($in, $pos1+1, $pos3-$pos1-1));
   $pos = $pos3+2;
  }
  elseif($pos2 && ($pos3 = StrPos($in, '.]', $pos2))){  // OBJEKT
   $out .= SynText(SubStr($in, $pos, $pos2-$pos-1));
   $out .= SynObjekt(SubStr($in, $pos2+1, $pos3-$pos2-1));
   $pos = $pos3+2;
  }
  else{
   $out .= SynText(SubStr($in, $pos));
   break;
  }
 }
 return $out;
}

//==============================================================================
function SynArtikel($in){ global $g;
 $in   = "\n".preg_replace("!(\r\n)|(\r)!", "\n", $in);
 $pos1 = 0;
 $n    = 0;

 while(1){
  $n++;

  if($pos2 = StrPos($in, '[=', $pos2))
   $out .= SynKapitel(SubStr($in, $pos1, $pos2-$pos1-1));
  else{
   $out .= SynKapitel(SubStr($in, $pos1));
   break;
  }

  $pos1 = $pos2;
  if($pos2 = StrPos($in, '=]', $pos1))
   $kapitel[$n] = SubStr($in, $pos1+2, $pos2-$pos1-2);
  else
   break;

  $out .= "
<table width='100%' cellpadding='0' cellspacing='0'>
 <tr>
  <td><nobr><a name='$n'><img src='img/k_begin.gif' width='4' height='15' border='0'></a><a href='#begin'><img src='img/k_up.gif' width='14' height='15' border='0'></a><a href='#end'><img src='img/k_down.gif' width='14' height='15' border='0'></a><img src='img/k_mittle.gif' width='15' height='15' border='0'></nobr></td>
  <td><nobr><u><strong>&nbsp;$kapitel[$n]&nbsp;</strong></u></nobr></td>
  <td width='100%'><img src='img/k_mittle.gif' border='0' width='100%' height='15'></td>
  <td><nobr><small><strong><u>&nbsp;bearbeiten&nbsp;</u></strong></small></nobr></td>
  <td><nobr><img src='img/k_mittle.gif' border='0' width='8' height='15'><img src='img/k_end.gif' width='4' height='15' border='0'></nobr></td>
 </tr>
</table>
 ";
  $pos1 = $pos2+2;
 }

 if($kapitel[1]){
  $out2 = "
<table width='100%' cellpadding='0' cellspacing='0'>
 <tr>
  <td valign='top'><img src='img/inhalt.gif' width='19' height='70'></td>
  <td width='100%'>
   <table width='100%' height='72' cellpadding='0' cellspacing='0'>
    <tr><td><img src='img/r_w.gif' width='100%' height='10'></td>
    <tr>
     <td valign='top' height='65'>
      <table>
       <tr valign='top'>
        <td>
        </td><td>";

  $max = 2;
  while(1){
   $max++;
   $l  = 0;
   $l_ = 0;
   $n  = 1;
   while($kapitel[$n]){
    if($l_ < StrLen($kapitel[$n]))
     $l_ = StrLen($kapitel[$n]);
    if(($l+$l_) >= 80)
     break;
    if(($n%$max) == 0){
     $l += $l_;
     $l_ = 0;
    }
    $n++;
   }
   if(($l+$l_) < 80)
    break;
  }

  $n = 1;
  while($kapitel[$n]){
   $out2 .= "\n<u><b><a href='#$n'><img src='img/kapitel.gif' align='top' width='14' height='15' border='0'>$kapitel[$n]</a></b></u> <br>";
   if(($n%$max) == 0)
    $out2 .= "\n</td><td>";
   $n++;
  }
  $out2 .= "
        </td>
       </tr>
      </table>
     </td>
    </tr>
   </table>
  </td>
 </tr>
</table>\n";
 }
 return $out2.$out."\n";
}


?>