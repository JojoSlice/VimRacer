# Spelplan — MonoGame 2D Multiplayer

---

## Översikt

Ett top-down 2D vertikalt scrollande racingspel för två spelare online. Spelarna rasar längs samma vertikala bana mot en mållinje och försöker hindra varandra med skott och knuffar. En rytmisk inmatningsuppgift (a/s/d/f-kombinationer) styr spelarens max-fart och ger ammunition.

---

## Spelmekanik

### Rörelse

Spelaren är **alltid i rörelse** längs den vertikala banan.

| Tangent | Funktion                       |
|---------|--------------------------------|
| h       | Sidledsrörelse vänster         |
| l       | Sidledsrörelse höger           |
| j       | Minska fart (ned mot min-fart) |
| k       | Öka fart (upp mot max-fart)    |

- Banan är **vertikal** med en viss bredd — inga kurvor
- Spelaren scrollar nedåt längs banan i sin nuvarande fart
- h/l förflyttar spelaren sidledes för att undvika hinder, väggar och motspelaren
- Spelaren har alltid en **min-fart** (kan aldrig stanna) och en **max-fart** (tak styrt av kombinationssystemet)

### Fart — 10 max-nivåer

- **Max-fart** har 10 nivåer (1 = lägsta tak, 10 = högsta tak)
- **Min-fart** är fast — spelaren kan aldrig stanna
- j/k justerar farten inom [min, max]

**Händelser som påverkar max-farten:**

| Händelse                  | Effekt på max-fart | Effekt på combo |
|---------------------------|--------------------|-----------------|
| Rätt kombination          | +1 nivå (max 10)   | +1              |
| Fel kombination           | −1 nivå (min 1)    | Reset → 0       |
| Kollision med hinder/vägg | −1 nivå (min 1)    | Reset → 0       |
| Träffas av skott          | −1 nivå (min 1)    | Reset → 0       |

### Kombinationssystem — a/s/d/f

- Spelet visar en sekvens av tangenter (a, s, d, f) på skärmen
- Spelaren matar in sekvensen **parallellt** med rörelse
- Krav-tid och kombinationslängd skalas med nuvarande **max-fart-nivå** — högre nivå = kortare tid och längre sekvens
- **5 rätt i rad** → spelaren tjänar ett skott (max 1 i lager)

**Krav-tabell:**

| Max-fart-nivå | Krav-tid per kombination | Kombinationslängd |
|---------------|--------------------------|-------------------|
| 1             | 3.0 s                    | 3 tangenter       |
| 2             | 2.7 s                    | 3 tangenter       |
| 3             | 2.4 s                    | 4 tangenter       |
| 4             | 2.1 s                    | 4 tangenter       |
| 5             | 1.8 s                    | 5 tangenter       |
| 6             | 1.5 s                    | 5 tangenter       |
| 7             | 1.2 s                    | 6 tangenter       |
| 8             | 0.9 s                    | 6 tangenter       |
| 9             | 0.6 s                    | 7 tangenter       |
| 10            | 0.4 s                    | 7 tangenter       |

### Hinder

- Hinder och väggar placerade vid varierande horisontella positioner på banan
- Spelaren undviker dem med h/l och j (bromsa för att hinna reagera)
- Kollision → max-fart −1, combo reset

### Knuffmekanik (spelare mot spelare)

- Spelarna befinner sig på **samma bana** och kan fysiskt kollidera
- Vid kollision **studsar spelarna ifrån varandra** — ingen tar skada
- Ren fysikbaserad interaktion: användbar för att blockera motspelaren eller ta genvägar

### Skjutsystem

- Skjut med **Space**
- Max **1 skott** i lager; tjänas in via 5 rätt i rad
- Skottet färdas alltid **framåt** (nedåt längs banan, i spelarens riktning)
- Träff på motspelaren → max-fart −1, combo reset
- Skott blockeras av hinder/väggar

---

## Vinst-villkor

- Spelaren som **först når mållinjen** i slutet av banan vinner
- Knuffar och skott är verktyg för att försinka motspelaren snarare än att eliminera

---

## Multiplayer — Online

- 2 spelare på samma vertikala bana
- **Arkitektur: Client-Server**
  - En spelare hostar, den andra ansluter (eller dedikerad server)
  - LiteNetLib rekommenderas som nätverksbibliotek

**State som synkroniseras:**
- Spelarposition (x, y) och aktuell fart
- Max-fart-nivå och combo-räknare
- Nuvarande kombination på skärmen
- Skott-position och status
- Kollisionshändelser

---

## Arena

- Vertikal scrollande bana med fast bredd
- Hinder och väggar vid varierande positioner längs banan
- Mållinje i slutet
- Layout: fast per match (möjlig expansion till procedurell generering senare)

---

## HUD per spelare

- Nuvarande kombination att mata in (a/s/d/f-sekvens)
- Combo-räknare (0–5+)
- Fart-mätare (visar nuvarande fart och max-fart-nivå)
- Skott-indikator (har skott / tomt)
- Motståndares position relativt spelaren (minimap eller indikator)

---

## Teknisk stack

- **Engine:** MonoGame (C#)
- **Nätverksbibliotek:** LiteNetLib

**Projektstruktur:**
```
/
├── Entities/
│   ├── Player.cs
│   ├── Bullet.cs
│   └── Obstacle.cs
├── Systems/
│   ├── InputSystem.cs
│   ├── ComboSystem.cs
│   ├── MovementSystem.cs
│   ├── CollisionSystem.cs
│   └── NetworkSystem.cs
├── Scenes/
│   ├── MenuScene.cs
│   ├── LobbyScene.cs
│   ├── GameScene.cs
│   └── ResultScene.cs
├── UI/
│   ├── ComboDisplay.cs
│   ├── SpeedMeter.cs
│   └── HUD.cs
└── Game1.cs
```

---

## Implementeringsordning

1. MonoGame-projekt setup
2. Vertikal rörelse + fart-system (j/k) + sidledsrörelse (h/l)
3. Kombinationssystem + HUD
4. Hinder + kollision (vägg och spelare)
5. Knuffmekanik (studs spelare mot spelare)
6. Skjutsystem
7. Nätverkskod + lobby (LiteNetLib, client-server)
8. Mållinje + vinst-skärm
9. Arena/bana-design
10. Polering: ljud, effekter, meny
