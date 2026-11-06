# Plan testiranja — Windows

## Ručna provjera na fizičkom Windows računalu

- [ ] WPF Release build prolazi na Windows uz .NET 10.
- [ ] Prazan registar: sučelje se otvara, 0 USB ključeva.
- [ ] Registracija prvog i drugog USB sticka daje različite ID-eve.
- [ ] Ponovna registracija istog USB-a odbijena je bez prepisivanja datoteke.
- [ ] Spojen USB #1: status prepoznat.
- [ ] Uklanjanje USB #1 uz priključen USB #2 ne zaključava računalo.
- [ ] Uklanjanje posljednjeg registriranog USB-a uz auto-lock uključeno zaključava sesiju nakon tri uzastopne neuspjele provjere.
- [ ] Bez prethodno prepoznatog ključa nema automatskog zaključavanja.
- [ ] Auto-lock je po zadanim postavkama isključen.
- [ ] Isključen auto-lock: uklanjanje USB-a ne zaključava.
- [ ] Opoziv onemogućuje buduće prepoznavanje USB-a.
- [ ] Promjena naziva ne mijenja dokaz; događaj je evidentiran.
- [ ] Oštećen USB token ignorira se.
- [ ] Oštećen DPAPI registar uzrokuje vidljivu grešku, nikada tihi reset.
- [ ] Postojeća Windows lozinka ili Hello omogućuje prijavu nakon zaključavanja.
- [ ] Windows prijava ne zahtijeva USB ako je aplikacija uklonjena.
- [ ] Standardni korisnik bez administratorskih ovlasti može upravljati vlastitim USB-ovima.
- [ ] ZIP i zapisnici ne sadrže tokene i Windows vjerodajnice.

## Automatizacija

GitHub Actions izvodi WPF Release build, smoke testove USB tokena i generira nepotpisani Portable EXE, ZIP, USB setup kit i SHA-256 sume; na main grani pokušava objaviti GitHub prerelease. Automatski build nije zamjena za testiranje na fizičkom Windows računalu ili neovisnu sigurnosnu provjeru.

**Ne testiraj automatsko zaključavanje ako nisi provjerio da znaš Windows lozinku ili Hello.** BitLocker oporavak je odvojen.
