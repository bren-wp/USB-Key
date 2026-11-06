# Brendigo USB Key — početna lozinka i oporavak

## Prvo pokretanje
Početna lozinka za upravljanje aplikacijom je **Brendigo**. Upiši je samo pri prvom postavljanju, a zatim obavezno postavi svoju lozinku od najmanje **12 znakova**. Početna lozinka nakon toga više ne vrijedi.

Ovo **nije Windows lozinka**. Ne mijenja Windows Hello PIN, BitLocker ni prijavu u Windows. Obični USB stick još ne otključava Windows.

## Svakodnevni pristup
Prilikom ručnog otvaranja upravljačke ploče ili ponovnog otvaranja iz Windows system traya upiši privatnu lozinku. Zatvaranjem glavnog prozora upravljanje se zaključava, ali USB nadzor u pozadini može nastaviti raditi. Nakon pet pogrešnih unosa program privremeno odgađa daljnje pokušaje. Ta zaštita vrijedi tijekom rada procesa i nije zamjena za sustavsku Windows autentifikaciju.

## Promjena lozinke
Otvori **Postavke → Promijeni lozinku aplikacije**. Upiši trenutačnu lozinku, novu (12–128 znakova) i potvrdu. Ako je promjena uspješna, stara više ne vrijedi. Nova lozinka provjerava se PBKDF2-SHA256 funkcijom s nasumičnom soli; lokalni zapis dodatno je zaštićen DPAPI CurrentUser mehanizmom.

## Gubitak USB-a
Prijavi se u Windows svojom Windows lozinkom/Hello načinom, zatim otvori aplikaciju svojom privatnom lozinkom, opozovi USB i registriraj drugi fizički stick.

## Zaboravljena lozinka aplikacije
Windows prijava ostaje neovisna. Početna Brendigo lozinka nije trajni univerzalni rezervni pristup. Lokalni zapis lozinke nalazi se u korisničkoj Windows mapi LocalAppData/Brendigo/USBKey/access.dat; registar USB ključeva zasebno je u registry.dat. Prije oporavka sačuvaj sigurnosnu kopiju podataka i **ne briši registry.dat** zbog lozinke aplikacije.

Napomena: osoba ili zlonamjerni program koji već može mijenjati datoteke tvojeg Windows profila može zaobići ograničenje lokalnog sučelja. Zato nije riječ o zamjeni za Windows prijavu ni hardversku autentifikaciju.

Autor: Brendigo · https://brendigo.com · MIT.
