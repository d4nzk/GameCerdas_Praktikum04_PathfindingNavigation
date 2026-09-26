# GameCerdas_Praktikum04_PathfindingNavigation

## Identitas

| Keterangan | Isi |
|---|---|
| Nama Kelompok | Xx_Sigm4_M4le_67_xX |
| Anggota 1 | Muhammad Farrel Fathin Wibowo / 5025231233 |
| Anggota 2 | Danny Rachmadian Yusuf Satryatama / 5025231240 |
| Anggota 3 | Valensio Arvin Putra Setiawan / 5025231273 |

## Praktikum: Pathfinding & Navigation — A\* Manual dan Unity NavMesh

### Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat NPC yang mampu mencari dan mengikuti jalur menuju tujuan, pertama dengan A\* yang dibuat sendiri pada grid, kemudian dengan sistem navigasi bawaan Unity (NavMesh).

Mahasiswa diharapkan mampu:

1. Merepresentasikan area game sebagai grid/graph.
2. Menentukan node walkable dan obstacle.
3. Menentukan neighbor dari sebuah node.
4. Mengimplementasikan A\* secara manual.
5. Menjelaskan fungsi `gCost`, `hCost`, dan `fCost`.
6. Menyimpan parent node dan melakukan reconstruct path.
7. Memvisualisasikan hasil pathfinding di Unity.
8. Membuat agent mengikuti path hasil A\*.
9. Menggunakan package AI Navigation pada Unity 6.
10. Membuat dan melakukan bake `NavMeshSurface`.
11. Menggerakkan NPC menggunakan `NavMeshAgent`.
12. Menggunakan `NavMeshObstacle` untuk obstacle dinamis.
13. Memahami perbedaan A\* manual dan Unity NavMesh.
14. Melakukan debugging ketika path atau NavMesh tidak bekerja.

### Fitur

Praktikum terbagi menjadi dua bagian yang terpisah, masing-masing dengan scene sendiri.

**Bagian A — A\* Manual** (`Assets/Scenes/P04_AStarGrid.unity`)

- Grid 10×10 dengan node walkable dan obstacle yang dideteksi otomatis dari layer.
- A\* manual dengan visualisasi Open Set, Closed Set, dan final path.
- Agent yang berjalan mengikuti path hasil A\*.
- Challenge: movement diagonal, terrain dengan cost berbeda (Mud dan Road), dan click destination.

**Bagian B — Unity NavMesh** (`Assets/Scenes/P04_NavMesh.unity`)

- `NavMeshSurface` yang di-bake dari scene dengan beberapa dinding.
- NPC dengan `NavMeshAgent` yang mengejar `PlayerTarget` yang digerakkan dengan keyboard.
- Visualisasi path dan log status agent untuk debugging.
- Challenge: dynamic obstacle dengan carving, multiple NavMeshAgent, dan NavMeshLink antar-area navigasi.

> Keterangan di atas hanya preview singkat pencapaian praktikum. Penjelasan lengkap ada di bagian berikutnya.

---

## Bagian A — A\* Manual

### Setup Scene

- **AStarSystem** menyimpan `GridManager`, `AStarPathFinder`, dan `ClickGoalSetter`.
- **StartMarker** dan **GoalMarker** menandai titik awal dan tujuan.
- Objek obstacle (bentuk L, T, dan lainnya) berada pada layer **Obstacle**. Petak **Mud** dan **Road** berada pada layer masing-masing, sedangkan **Ground** berada pada layer **Ground** untuk deteksi klik mouse.
- **Agent** memakai `AgentPathFollower` untuk berjalan mengikuti path.

Arti warna tile pada grid:

| Warna | Arti |
|---|---|
| Putih | Node walkable (terrain Normal) |
| Coklat | Node Mud (cost tinggi) |
| Abu-abu | Node Road (cost rendah) |
| Hitam | Obstacle (tidak walkable) |
| Kuning | Node di Open Set (sudah ditemukan, belum diperiksa) |
| Oranye | Node di Closed Set (sudah diperiksa) |
| Cyan | Final path |
| Hijau / Merah | Node start / node goal |

### Penjelasan Logika Kode

Penjelasan mengikuti urutan baris pada masing-masing file: `GridNode`, `GridManager`, `AStarPathFinder`, `AgentPathFollower`, lalu `ClickGoalSetter`.

#### 1. `Assets/Scripts/AStar/GridNode.cs`

**`TerrainType`**
Daftar jenis terrain: `Normal`, `Mud`, dan `Road`. Setiap jenis punya biaya lewat yang berbeda. Ini bagian dari challenge terrain dengan cost berbeda.

**Field `GridNode`**
Satu `GridNode` mewakili satu kotak pada grid:
- `x`, `y`: posisi kotak di dalam grid, dan `worldPosition`: posisi kotak di dunia game.
- `walkable`: apakah kotak bisa dilewati atau berisi obstacle.
- `terrainType` dan `movementCost`: jenis terrain dan biaya untuk masuk ke kotak itu.
- `gCost`: biaya perjalanan dari start sampai kotak ini.
- `hCost`: perkiraan biaya dari kotak ini sampai goal.
- `parent`: kotak sebelumnya pada jalur terbaik, dipakai untuk menyusun path di akhir.
- `visual`: tile yang ditampilkan di scene untuk kotak ini.

**`FCost`**
Total nilai kotak, yaitu `gCost + hCost`. A\* selalu memilih kotak dengan `FCost` terkecil untuk diperiksa lebih dulu.

**Constructor**
Mengisi data kotak dan menyiapkan nilai awal pencarian: `gCost` dibuat sangat besar (belum pernah dicapai), `hCost` nol, dan `parent` kosong.

#### 2. `Assets/Scripts/AStar/GridManager.cs`

**Field.** Ukuran grid (`width`, `height`) dan ukuran kotak (`cellSize`), layer obstacle (`obstacleMask`), layer terrain (`mudMask`, `roadMask`) beserta biayanya (`normalCost` 10, `mudCost` 30, `roadCost` 5), serta pengaturan dan warna visualisasi.

**`Awake()`**
Membuat grid sekali saat game mulai, sehingga grid sudah siap sebelum A\* dijalankan di `Start()`.

**`CreateGrid()`**
1. Menyiapkan array node dan satu objek induk untuk menampung semua tile visual.
2. Untuk setiap kotak, dihitung posisinya di dunia, lalu diperiksa dengan kotak fisika kecil (`Physics.CheckBox`) apakah ada obstacle di atasnya. Jika ada, kotak itu **tidak walkable**.
3. Jenis terrain kotak ditentukan dan biayanya diambil, lalu `GridNode` dibuat dan disimpan ke grid.
4. Jika `showGrid` aktif, tile visual dibuat untuk kotak tersebut.

**`GetWorldPosition(x, y)`**
Mengubah posisi grid menjadi posisi dunia: posisi `GridManager` ditambah `x` dan `y` dikali `cellSize`.

**`CreateVisual(node)`**
Membuat cube tipis sebagai tile di posisi node, menghapus collider-nya agar tidak mengganggu fisika dan klik mouse, lalu mewarnainya: hitam jika obstacle, atau warna terrain jika walkable.

**`GetTerrainType(worldPosition)`**
Memeriksa objek di bawah kotak. Jika mengenai layer Road, terrain-nya Road; jika mengenai layer Mud, terrain-nya Mud; selain itu Normal. Ini bagian dari challenge terrain dengan cost berbeda.

**`GetTerrainCost(terrainType)`** dan **`GetTerrainColor(terrainType)`**
Mengembalikan biaya dan warna yang sesuai dengan jenis terrain.

**`GetMinimumTerrainCost()`**
Mengembalikan biaya terrain termurah (minimal 1). Nilai ini dipakai heuristic agar perkiraan jarak ke goal tidak pernah lebih besar dari biaya sebenarnya.

**`NodeFromWorldPosition(worldPosition)`**
Kebalikan dari `GetWorldPosition`: mencari node mana yang berada di posisi dunia tertentu, misalnya posisi StartMarker dan GoalMarker. Hasilnya dibatasi agar tidak keluar dari grid.

**`GetNeighbors(node)`**
Mengumpulkan tetangga sebuah node: 4 arah lurus (kanan, kiri, atas, bawah) dan 4 arah diagonal. Tetangga diagonal merupakan challenge A\* dengan movement diagonal.

**`TryAddNeighbor(...)`**
Menambahkan tetangga lurus jika posisinya masih di dalam grid. Status walkable diperiksa nanti oleh A\*.

**`TryAddDiagonalNeighbor(...)`**
Menambahkan tetangga diagonal hanya jika masih di dalam grid **dan** kedua kotak lurus yang mengapitnya walkable. Tujuannya agar agent tidak memotong sudut obstacle (corner cutting).

**`ResetSearchData()`**
Mengembalikan `gCost`, `hCost`, dan `parent` semua node ke nilai awal serta mengembalikan warna tile, supaya pencarian baru tidak terpengaruh hasil pencarian sebelumnya.

**`SetNodeColor(node, color)`**
Mengubah warna tile sebuah node. Dipakai untuk menampilkan Open Set, Closed Set, dan final path.

#### 3. `Assets/Scripts/AStar/AStarPathFinder.cs`

**Field.** Referensi `GridManager`, `startMarker`, `goalMarker`, toggle `showOpenClosed` untuk visualisasi, dan `currentPath` sebagai hasil path yang dibaca oleh agent.

**`Start()`**
Menjalankan `FindPath()` sekali saat game mulai.

**`FindPath()`**
Fungsi utama A\*. Dapat juga dijalankan manual lewat menu klik kanan komponen (**Find Path**).
1. **Validasi.** Jika referensi belum diisi, muncul peringatan dan pencarian dibatalkan.
2. **Persiapan.** Data pencarian di-reset, lalu node start dan node goal dicari dari posisi marker. Jika salah satunya berada di obstacle, muncul peringatan dan pencarian dibatalkan.
3. **Inisialisasi.** Dibuat **Open Set** (node yang sudah ditemukan tetapi belum diperiksa) dan **Closed Set** (node yang sudah selesai diperiksa). Node start diberi `gCost` 0, dihitung `hCost`-nya, lalu dimasukkan ke Open Set.
4. **Perulangan utama**, selama Open Set belum kosong:
   - Node dengan `FCost` terkecil diambil dari Open Set, dipindahkan ke Closed Set, dan diwarnai oranye.
   - Jika node itu adalah goal, path disusun dengan `ReconstructPath()`, ditampilkan dengan `VisualizeFinalPath()`, lalu pencarian selesai.
   - Jika bukan, setiap tetangganya diperiksa. Tetangga yang obstacle atau sudah di Closed Set dilewati.
   - Biaya langkah ke tetangga dihitung dari `movementCost` terrain tetangga itu. Langkah diagonal dikali 1,4 (karena lebih panjang dari langkah lurus); ini bagian dari challenge diagonal dan terrain cost.
   - Jika jalur lewat node sekarang lebih murah dari `gCost` tetangga sebelumnya, tetangga itu diperbarui: `parent` diisi node sekarang, `gCost` dan `hCost` diisi, lalu dimasukkan ke Open Set (jika belum ada) dan diwarnai kuning.
5. Jika Open Set habis sebelum goal ditemukan, berarti goal tidak dapat dicapai: path dikosongkan dan muncul peringatan **Path tidak ditemukan**.

**`GetLowestFCostNode(openSet)`**
Mencari node di Open Set dengan `FCost` terkecil. Jika ada yang sama, dipilih yang `hCost`-nya lebih kecil (lebih dekat ke goal), sehingga pencarian lebih cepat mengarah ke tujuan.

**`GetHeuristic(a, b)`**
Menghitung perkiraan biaya dari node ke goal. Karena grid memakai 8 arah, dipakai jarak diagonal (octile): sebanyak mungkin langkah diagonal (biaya 14), sisanya langkah lurus (biaya 10). Hasilnya disesuaikan dengan biaya terrain termurah, sehingga perkiraan tidak pernah melebihi biaya sebenarnya dan path yang ditemukan tetap yang termurah.

**`ReconstructPath(startNode, goalNode)`**
Menyusun path dengan berjalan mundur dari goal mengikuti `parent` setiap node sampai tiba di start. Karena urutannya goal → start, list tersebut dibalik agar menjadi start → goal.

**`VisualizeFinalPath(startNode, goalNode)`**
Mewarnai semua node pada path dengan cyan, node start hijau, dan node goal merah.

#### 4. `Assets/Scripts/AStar/AgentPathFollower.cs`

**Field.** Referensi `AStarPathFinder`, kecepatan gerak (`moveSpeed`), kecepatan menoleh (`rotationSpeed`), jarak dianggap sampai di waypoint (`waypointThreshold`), serta `path` dan `currentIndex` (waypoint yang sedang dituju).

**`Start()`**
Jika pathfinder tersedia, agent disiapkan lewat `RestartFromStart()`.

**`RestartFromStart()`**
Memindahkan agent ke posisi StartMarker, mengambil path terbaru dari pathfinder, dan mulai lagi dari waypoint pertama.

**`Update()`**
1. Jika pathfinder menghasilkan path baru, agent memakai path itu dan mulai dari waypoint pertama.
2. Jika tidak ada path atau semua waypoint sudah dilewati, agent diam.
3. Waypoint yang dituju adalah posisi node pada path (ketinggian disamakan dengan agent).
4. Jika jarak ke waypoint sudah di bawah `waypointThreshold`, agent pindah ke waypoint berikutnya.
5. Jika belum, agent digerakkan menuju waypoint dengan kecepatan `moveSpeed`, lalu diputar perlahan menghadap arah geraknya.

#### 5. `Assets/Scripts/AStar/ClickGoalSetter.cs`

Script ini merupakan challenge click destination.

**Field.** Kamera, GoalMarker, pathfinder, agent, dan `groundMask` (layer lantai yang boleh diklik).

**`Update()`**
1. Menunggu klik kiri mouse.
2. Menembakkan ray dari kamera ke posisi kursor. Ray hanya mengenai layer Ground.
3. Jika mengenai lantai, GoalMarker dipindah ke titik klik, A\* dijalankan ulang, lalu agent dikembalikan ke start untuk mengikuti path yang baru.

### Dokumentasi A\*

**Grid dan obstacle**

![Grid dan obstacle](Docs/Screenshots/AStar/01_grid_obstacle.png)

**Open Set, Closed Set, dan final path**

![Open Set, Closed Set, dan final path](Docs/Screenshots/AStar/02_open_closed_path.png)

**Agent mengikuti path**

![Agent mengikuti path](Docs/Screenshots/AStar/03_agent_follow_path.png)

---

## Bagian B — Unity NavMesh

### Setup Scene

Bagian ini memakai package **AI Navigation** (`com.unity.ai.navigation` 2.0.15).

- **Navigation** menyimpan dua komponen:
  - `NavMeshSurface` yang mengumpulkan semua objek di scene lalu di-bake menjadi area yang bisa dilalui (NavMesh). NavMeshAgent dan NavMeshObstacle diabaikan saat bake, karena keduanya bergerak.
  - `NavMeshLink` yang menghubungkan **Ground** (area utama) dengan **Ground_02** (area terpisah di seberang celah). Link ini dua arah, sehingga NPC bisa menyeberang bolak-balik. Ini merupakan challenge NavMeshLink antar-area navigasi.
- **wall_01**, **wall_02**, **wall_03** adalah dinding statis yang ikut di-bake sebagai penghalang.
- **NPC** (mulai di Ground_02) dan **NPC_02** (mulai di Ground) masing-masing memakai `NavMeshAgent`, `NavMeshChaser`, dan `NavMeshPathDebugger`, dan keduanya mengejar `PlayerTarget`. Ini merupakan challenge multiple NavMeshAgent. Pengaturan agent: `Speed` 3.5, `Acceleration` 8, `Radius` 0.5, dan `Stopping Distance` 1.5.
- **DynamicObstacle_1** dan **DynamicObstacle_2** adalah dinding yang memakai `NavMeshObstacle` (bentuk Box, **Carve** aktif) dan `Rigidbody`, sehingga bisa didorong jatuh oleh `PlayerTarget`. Saat posisinya berubah, lubang pada NavMesh ikut berpindah dan NPC mencari jalan baru. Ini merupakan challenge dynamic obstacle.
- **PlayerTarget** digerakkan dengan keyboard dan memakai `Rigidbody` (rotasi dikunci) agar dapat mendorong dynamic obstacle.

### Penjelasan Logika Kode

Penjelasan mengikuti urutan: `PlayerTargetMovement`, `NavMeshChaser`, lalu `NavMeshPathDebugger`.

#### 1. `Assets/Scripts/NavMesh/PlayerTargetMovement.cs`

**Field.** Kecepatan gerak (`moveSpeed`).

**`Update()`**
1. Membaca tombol WASD atau panah menjadi arah horizontal dan vertical.
2. Arah dinormalisasi agar gerak diagonal tidak lebih cepat.
3. PlayerTarget digerakkan sejauh `arah × moveSpeed × Time.deltaTime`.

#### 2. `Assets/Scripts/NavMesh/NavMeshChaser.cs`

**Field.** Target yang dikejar, `repathInterval` (jeda minimal antar perhitungan path, 0.25 detik), `targetMoveThreshold` (jarak minimal target berpindah sebelum path dihitung ulang, 0.5 unit), referensi `NavMeshAgent`, waktu repath berikutnya, dan posisi target terakhir.

**`Awake()`**
Mengambil komponen `NavMeshAgent` di objek yang sama.

**`Start()`**
Jika target ada, posisi target dicatat dan tujuan agent langsung diatur ke posisi target.

**`Update()`**
Mengatur kapan path dihitung ulang supaya tidak terjadi setiap frame:
1. Jika belum waktunya repath, tidak melakukan apa pun.
2. Jika sudah waktunya, dihitung seberapa jauh target berpindah sejak tujuan terakhir diatur. Tujuan hanya diperbarui jika target berpindah minimal `targetMoveThreshold`.
3. Waktu repath berikutnya dijadwalkan.

**`UpdateDestination()`**
1. Jika agent tidak berada di atas NavMesh, muncul peringatan dan fungsi berhenti. Ini membantu debugging ketika NPC diletakkan di luar area bake.
2. Jika aman, tujuan agent diatur ke posisi target dengan `SetDestination()`. Selanjutnya `NavMeshAgent` sendiri yang mencari path dan menggerakkan NPC.
3. Posisi target dicatat untuk perbandingan berikutnya.

#### 3. `Assets/Scripts/NavMesh/NavMeshPathDebugger.cs`

**Field.** Referensi `NavMeshAgent`, warna garis path (`lineColor`), dan jeda antar log (`logInterval`).

**`Awake()`**
Mengambil komponen `NavMeshAgent`.

**`Update()`**
Setiap `logInterval` detik, menulis status agent ke Console: apakah path masih dihitung (`pathPending`), apakah agent punya path (`hasPath`), sisa jarak (`remainingDistance`), dan status path (`pathStatus`: complete, partial, atau invalid). Log ini dipakai untuk mencari tahu penyebab NPC tidak bergerak.

**`OnDrawGizmos()`**
Jika agent punya path, titik-titik belok path (`corners`) digambar sebagai bola kecil dan dihubungkan dengan garis cyan, sehingga rute yang dipilih NavMesh terlihat di Scene view.

### Dokumentasi NavMesh

**Hasil Bake NavMesh**

![Hasil Bake NavMesh](Docs/Screenshots/NavMesh/01_bake_navmesh.png)

**NPC memutari obstacle menuju target**

![NPC memutari obstacle menuju target](Docs/Screenshots/NavMesh/02_npc_detour.png)

**Visualisasi path**

![Visualisasi path](Docs/Screenshots/NavMesh/03_path_visualization.png)

**Eksperimen NavMeshObstacle (Carve = Off)**

![Eksperimen NavMeshObstacle](Docs/Screenshots/NavMesh/04_navmesh_obstacle.png)

**Eksperimen NavMeshObstacle (Carve = On)**

![Eksperimen NavMeshObstacle](Docs/Screenshots/NavMesh/05_navmesh_obstacle.png)

---

## Perbandingan A\* Manual dan Unity NavMesh

| Aspek | A\* Manual | Unity NavMesh |
|---|---|---|
| Representasi area | Grid kotak berukuran tetap | Poligon hasil bake dari geometri scene |
| Obstacle | Dideteksi per kotak lewat layer | Terbentuk dari geometri saat bake, atau `NavMeshObstacle` saat runtime |
| Pencarian path | Ditulis sendiri (Open Set, Closed Set, `gCost`, `hCost`) | Dilakukan oleh `NavMeshAgent` |
| Pergerakan | Ditulis sendiri (`AgentPathFollower`) | Otomatis oleh `NavMeshAgent`, termasuk menghindari agent lain |
| Kelebihan | Mudah dipahami, cost terrain bebas diatur | Path lebih halus, cepat, siap dipakai untuk scene besar |
| Kekurangan | Path kaku mengikuti kotak, lambat untuk grid besar | Perlu di-bake ulang untuk perubahan statis, lebih sulit dikustomisasi |

---

## Pertanyaan Analisis

### 1. Mengapa Seek saja tidak cukup untuk scene yang memiliki dinding besar?

Seek hanya bergerak lurus ke arah target tanpa mengetahui peta. Jika ada dinding besar di antaranya, NPC akan menabrak dan tertahan di dinding. Diperlukan pathfinding untuk merencanakan rute memutar.

### 2. Apa yang direpresentasikan oleh node pada grid?

Satu node mewakili satu kotak area di dunia game, lengkap dengan posisinya, status walkable atau obstacle, dan biaya untuk melewatinya.

### 3. Apa fungsi `gCost`?

Menyimpan biaya sebenarnya dari node start sampai node tersebut melalui jalur terbaik yang sudah ditemukan.

### 4. Apa fungsi `hCost`?

Menyimpan perkiraan (heuristic) biaya dari node tersebut sampai goal. Nilai ini mengarahkan pencarian agar condong ke goal.

### 5. Mengapa `fCost = gCost + hCost`?

Karena `fCost` adalah perkiraan total biaya jalur yang melewati node tersebut: biaya yang sudah ditempuh ditambah perkiraan sisa biaya. Dengan memilih `fCost` terkecil, A\* memeriksa node yang paling menjanjikan lebih dulu.

### 6. Mengapa Manhattan Distance cocok untuk grid 4 arah?

Pada grid 4 arah, agent hanya bisa bergerak horizontal atau vertikal, sehingga jumlah langkah minimum ke goal tepat `|dx| + |dy|`, yaitu Manhattan Distance. Perkiraannya tidak pernah melebihi biaya sebenarnya, sehingga path yang ditemukan tetap optimal. Project ini memakai gerak diagonal, sehingga heuristic diganti dengan jarak diagonal (octile), karena Manhattan akan melebihi biaya sebenarnya ketika agent bisa bergerak diagonal.

### 7. Apa fungsi `parent` pada `GridNode`?

Menyimpan node sebelumnya pada jalur terbaik menuju node tersebut. Setelah goal ditemukan, `parent` diikuti mundur dari goal ke start untuk menyusun path.

### 8. Mengapa hasil reconstruct path perlu dibalik?

Karena path disusun dengan mengikuti `parent` dari goal ke start, sehingga urutannya terbalik. Agent perlu berjalan dari start ke goal, jadi list tersebut dibalik.

### 9. Apa perbedaan open set dan closed set?

**Open Set** berisi node yang sudah ditemukan tetapi belum diperiksa tetangganya (kandidat berikutnya). **Closed Set** berisi node yang sudah selesai diperiksa dan tidak perlu diperiksa lagi.

### 10. Apa perbedaan pathfinding dan path following?

**Pathfinding** adalah proses mencari rute dari start ke goal (A\* atau NavMesh). **Path following** adalah proses menggerakkan agent menyusuri rute tersebut waypoint demi waypoint (`AgentPathFollower`, atau `NavMeshAgent` pada NavMesh).

### 11. Apa fungsi `NavMeshSurface`?

Menentukan area mana yang bisa dilalui agent, lalu melakukan bake geometri scene menjadi data NavMesh yang dipakai oleh `NavMeshAgent` untuk mencari path.

### 12. Apa fungsi `NavMeshAgent`?

Komponen pada NPC yang mencari path di atas NavMesh menuju tujuan (`SetDestination`) lalu menggerakkan NPC mengikuti path tersebut, termasuk mengatur kecepatan, belokan, pengereman, dan menghindari agent lain.

### 13. Apa fungsi `Stopping Distance`?

Jarak dari tujuan tempat agent berhenti. Tujuannya agar NPC tidak menabrak atau menumpuk dengan target. Pada project ini NPC berhenti 1.5 unit dari PlayerTarget.

### 14. Mengapa agent radius dapat menyebabkan lorong menjadi tidak dapat dilalui?

Saat bake, NavMesh dikecilkan sejauh radius agent dari setiap dinding agar badan agent tidak masuk ke dinding. Jika lebar lorong kurang dari dua kali radius, kedua sisi saling menutup sehingga lorong hilang dari NavMesh.

### 15. Apa perbedaan `NavMeshObstacle` dan collider biasa dalam konteks navigation?

Collider biasa hanya dipakai fisika. Jika objeknya bergerak saat game berjalan, NavMesh tidak mengetahuinya dan NPC tetap mencoba melewatinya. `NavMeshObstacle` dikenali oleh sistem navigasi, sehingga agent menghindarinya, dan dengan carving area tersebut dilubangi dari NavMesh.

### 16. Apa fungsi carving?

Carving melubangi NavMesh di area obstacle saat game berjalan, sehingga path dihitung ulang untuk memutari obstacle tersebut. Tanpa carving, agent hanya menghindar di sekitar obstacle dan bisa tertahan di depannya. Pada project ini lubang ikut berpindah ketika dynamic obstacle didorong.

### 17. Kapan `NavMeshLink` diperlukan?

Ketika dua area NavMesh tidak tersambung tetapi agent harus bisa berpindah, misalnya menyeberangi celah, melompat, turun dari ketinggian, atau berpindah ke permukaan lain. Pada project ini dipakai untuk menghubungkan Ground dengan Ground_02.

### 18. Mengapa path tidak sebaiknya dihitung ulang tanpa kontrol untuk semua NPC setiap frame?

Menghitung path cukup berat. Jika semua NPC melakukannya setiap frame, CPU terbebani dan game dapat patah-patah, padahal hasilnya sering tidak berubah. Karena itu `NavMeshChaser` hanya menghitung ulang setiap 0.25 detik dan hanya jika target berpindah minimal 0.5 unit.
