# Brendigo USB Key — rješavanje poteškoća

## Windows se zaključao, ali umetanje USB-a ga ne otključava

To je očekivano. U razvojnoj verziji 0.2 USB se koristi za lokalnu registraciju i opcionalno zaključavanje već otvorene Windows sesije, **ne** za prijavu u zaključani Windows. Koristi svoju Windows lozinku ili Windows Hello.

## USB nije ponuđen za registraciju

Program podržava samo spremne pogone koje Windows označi kao **Removable**. Provjeri prepoznaje li stick Windows Explorer, je li dopušteno pisanje te radi li drugi priključak. Neki vanjski USB diskovi prikazuju se kao **Fixed** i nisu podržani u ovoj verziji.

## Postoji key.json, ali program ne može registrirati uređaj

Postojeći token namjerno se ne prepisuje. Ako je USB već registriran, program odbija dupliciranu registraciju. Ako je token neispravan, koristi drugi fizički stick i sačuvaj vlastite podatke.

## Istodobno je umetnuto više USB stickova

Uklanjanje prvog USB-a ne bi smjelo zaključati Windows ako drugi registrirani ključ ostane prepoznat. Potrebna je provjera na stvarnim uređajima prije oslanjanja na tu funkciju.

## Auto-lock ne reagira

Aplikacija mora biti pokrenuta, a u njezinoj je sesiji prethodno morao biti prepoznat barem jedan registrirani USB. Nije instaliran sustavski servis za trajni nadzor.

## Program se ne pokreće s Windowsom

Opcija pokretanja nakon prijave vrijedi za trenutačnog Windows korisnika. Portable EXE treba biti na stabilnoj lokaciji na računalu, a ne na USB sticku koji može biti uklonjen.

## Sigurnosni registar nije moguće otvoriti

Podaci su u korisničkoj mapi LocalAppData, zaštićeni DPAPI-jem. Ne briši registar nasumično i ne kopiraj ga na drugi Windows profil. Windows lozinka i Hello ostaju neovisni o ovom registru.

## Antivirusni ili SmartScreen alarm

EXE nije digitalno potpisan. Preuzimaj ga samo s vlastitog GitHub repozitorija, provjeri SHA-256 i nemoj zaobilaziti organizacijske sigurnosne politike.

## BitLocker traži recovery key

Za pristup šifriranom disku koristi odgovarajući BitLocker ključ za oporavak. To nije isto što i Windows lozinka, Hello PIN ili Brendigo USB token.

Brendigo · https://brendigo.com
