# Brendigo USB Key 0.2 — testna Windows alfa

Autor i brend: Brendigo · https://brendigo.com · MIT

## Promjene

- Novi Splash/Welcome vodič s četiri interaktivna koraka i originalnom ikonom aplikacije.
- Redizajnirani dijalozi za registraciju i preimenovanje USB-a, uključujući osvježavanje popisa USB pogona.
- Windows system tray s nastavkom nadzora pri zatvaranju prozora i eksplicitnom naredbom za izlaz.
- PerMonitorV2 DPI manifest za kvalitetnije skaliranje sučelja na više zaslona.
- Naziv Windows izvršne datoteke usklađen s namjenom testiranja: Brendigo-USB-Key-Portable.exe.
- USB token provjerava se kroz strožu validaciju JSON-a i kanonskog Base64 zapisa.
- Osnovna zaštita od preusmjeravanja mapa i datoteka na USB-u (reparse-point provjera).
- Dodatna zaštita od kratkotrajnog nestanka USB uređaja prije automatskog zaključavanja.
- Rotacija lokalnog sigurnosnog dnevnika.
- Automatski smoke testovi formata tokena, Windows build i SHA-256 sume.
- Portable EXE, ZIP s programom i vodičima te USB Setup Kit koji sadrži Portable.exe, HR/EN dokumentaciju i opcionalnu Install-To-USB.ps1 skriptu s provjerom izmjenjivog pogona i SHA-256 kopije.
- 13 automatizirano provjerenih stvarnih WPF snimki zaslona i tri dodatne varijante u README-u i u ZIP paketu.

- Ponašanje auto-locka dodatno zaštićeno od neuspjelih USB očitanja; samo tri uzastopne pouzdane provjere odsutnosti mogu zatražiti zaključavanje.
- Uvedena stroža validacija lokalnog DPAPI registra i ograničeno čitanje tokena s izmjenjivog medija.
- Statusi USB ključeva na nadzornoj ploči prikazuju se zasebno; dodane su kontrole i provjera autostart lokacije.
- Provjereni WPF callbackovi, 55 sigurnosnih smoke testova i 13 stvarnih CI-renderiranih prikaza aplikacije uz tri varijante manjih prozora i DPI skaliranja.

- Ujednačene WPF pozadine i kontrast gumba te bolje ponašanje Dashboard kartica na manjim prozorima.
- Security Log omogućuje lokalno pretraživanje, osvježavanje i izvoz uz upozorenje o privatnosti.
- USB opoziv ima posebno dizajniran dijalog s obveznom izričitom potvrdom.
- Uvedene su provjere manjeg prozora, alternativnog WPF DPI-ja i rada potvrde opoziva.
- CI se sigurnije nosi s istovremenim nadogradnjama stvarnih WPF snimki na main.

- Dovršetak onboarding vodiča pamti se zasebno: korisnik ne mora registrirati USB da bi pri sljedećem pokretanju izravno vidio Dashboard.
- Security Log čita i posljednju rotiranu arhivu, a izvoz TXT-a uključuje sve trenutačno sačuvane događaje te odbija prepisivanje izvornih logova.
- Vektorske ikone u bočnoj navigaciji i početnom vodiču te suptilne 3D sjene kartica; dodana provjera oznaka navigacije nakon otkrivene regresije.
- Ručno osvježavanje nakon greške može ponovno uključiti periodični USB nadzor; nepouzdano očitanje nikada se ne tretira kao potvrđeni gubitak ključa.
- Dodane regresijske provjere i ukupno 55 USB/sigurnosnih smoke testova.

- Dashboard razlikuje nepouzdano USB očitanje od stvarnog uklanjanja i jasno prikazuje odgodu automatskog zaključavanja.
- Ojačana registracija USB-a: dopušten je isključivo korijen izmjenjivog pogona, a gumb za registraciju nije aktivan bez valjanog naziva i pogona.
- Osam novih regresijskih provjera za neprovjerljiv prethodno poznat USB i sedam provjera pravila registracije.

## Važno: ograničenja alfa verzije

**USB ne otključava Windows zaslon za prijavu.** Program podržava registraciju, detekciju i opcionalno zaključavanje već otvorene sesije. Windows lozinka/Hello ostaju rezervni način prijave. Obični USB token moguće je kopirati. FIDO2 i Credential Provider nisu implementirani.

Ovo je nepotpisano testno izdanje, **nije sigurnosno revidirano za produkcijsku autentifikaciju**. Windows build i smoke testovi ne dokazuju pixel-perfect vizualnu usklađenost niti zamjenjuju provjeru fizičkim USB uređajima.
