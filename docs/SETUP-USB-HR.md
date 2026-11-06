
# Brendigo USB Key — upute za tri i više USB stickova

**Razvojna verzija 0.2.0:** Ovaj program **ne otključava Windowsovu prijavu**. Podržava lokalnu USB registraciju i opcionalno zaključavanje već otvorene Windows sesije.

## Prije početka

Pripremi računalo s Windowsom 10/11, provjerenu Windows lozinku ili Windows Hello PIN te najmanje dva zasebna USB sticka. Ako koristiš BitLocker, prethodno spremi ključ za oporavak diska izvan računala. Preuzmi nepotpisanu testnu aplikaciju i USB setup kit s [GitHub Releases](https://github.com/bren-wp/USB-Key/releases); ZIP raspakiraj prije pokretanja.

## Registracija

1. Prijavi se u Windows uobičajenim načinom i pokreni aplikaciju na računalu.
2. Umetni prvi USB stick. Otvori **USB ključevi → Dodaj USB**.
3. Odaberi spreman izmjenjivi pogon i naziv **Glavni USB**, pa potvrdi registraciju.
4. Program na USB-u stvara skrivenu mapu **.brendigo-usb-key** i datoteku **key.json** sa zasebnim nasumičnim tokenom.
5. Izvadi prvi stick, umetni drugi i ponovi postupak pod nazivom **Rezervni USB**.
6. Odvojeno ponovi za treći stick i svaki dodatni uređaj.
7. Svaki stick umetni i provjeri pojavljuje li se kao spojen.
8. Za početak **ostavi auto-lock isključen**; uključi ga tek kad si provjerio da ti radi Windows prijava bez USB-a.

## Što staviti na USB?

Na svakom sticku potreban je samo njegov **zasebno generirani token**. Nije potrebno kopirati program na svaki stick. Možeš spremiti kopiju Windows portable paketa radi ručnog pokretanja nakon prijave u Windows, ali to **ne omogućuje USB prijavu na zaključanom zaslonu**. Windows neće automatski pokrenuti aplikaciju s USB-a na zaključanom računalu.

**Ne kopiraj istu datoteku key.json između USB-ova.** Time nastaje klon istoga ključa. Podaci na običnom USB-u mogu se kopirati, pa to nije zaštita usporediva s FIDO2 uređajem.

## Opcionalno: postavljanje portable programa na USB

Najnoviji **Brendigo-USB-Key-USB-Setup-Kit.zip** s [GitHub izdanja](https://github.com/bren-wp/USB-Key/releases) sadrži Portable.exe, upute i skriptu **Install-To-USB.ps1**. Raspakiraj ZIP na Windows računalu, umetni USB stick i po želji pokreni skriptu uz slovo pogona. Skripta provjerava je li pogon izmjenjiv, traži izričitu potvrdu **DA**, kopira EXE i upute te provjerava SHA-256 kopije.

Skripta **ne stvara USB token, ne mijenja Windows prijavu i ne otključava zaključani zaslon**. Program se mora ručno pokrenuti nakon prijave u Windows, a svaki USB zasebno registrirati u aplikaciji. Ako pravila tvog računala ne dopuštaju PowerShell skripte, možeš ručno kopirati Portable.exe bez zaobilaženja tih pravila.

## Ako se USB pokvari ili izgubi

1. Prijavi se Windows lozinkom ili Hello metodom.
2. Pokreni aplikaciju.
3. Opozovi oštećeni ili izgubljeni USB na stranici **USB ključevi**.
4. Registriraj drugi fizički USB kao zamjenu.
5. Ponovno provjeri postojeće rezervne stickove.

Opoziv na računalu ne briše automatski datoteku na sticku. BitLocker recovery key nije zamjena za Windows lozinku i treba se čuvati odvojeno.

## Rad u pozadini

Nakon prvog pokretanja možeš koristiti Windows obavijesno područje (system tray). Zatvaranjem glavnog prozora tipkom X aplikacija ostaje pokrenuta i nastavlja USB provjere; desnim klikom na njezinu ikonu odaberi **Zatvori aplikaciju** za potpuno gašenje. Ako uključiš automatsko pokretanje u Windowsu, prijava pokreće aplikaciju u pozadini, bez obveznog prolaska uvodnim vodičem.

## Rješavanje poteškoća

- **USB nije na popisu:** Provjeri prikazuje li ga Windows kao spreman *Removable* pogon, je li podržan i može li se na njega pisati. Neki vanjski diskovi označeni su kao *Fixed* i možda se neće prikazati.
- **USB nije prepoznat kao registriran:** Provjeri datoteku ključa, pokušaj drugi USB priključak i nemoj kopirati tuđi token.
- **Aplikacija nije pokrenuta:** Auto-lock i detekcija u ovoj verziji neće raditi kao stalni sustavski servis.
- **Zaključavanje radi, a USB ne otključava Windows:** To je očekivano u verziji 0.2.0. Koristi Windows lozinku/Hello.

## Preporučena testna konfiguracija

**Tri zasebno registrirana USB sticka + provjerena Windows lozinka ili Hello + auto-lock isključen dok testiranje ne završi.**

Autor i vlasnik: Brendigo · https://brendigo.com
