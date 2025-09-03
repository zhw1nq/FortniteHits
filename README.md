# FortniteHits Plugin for CounterStrikeSharp

Plugin hiển thị damage numbers như trong game Fortnite cho CS2.

## Cấu trúc thư mục

```
addons/counterstrikesharp/
├── configs/
│   └── plugins/
│       └── FortniteHits/
│           └── FortniteHits.json          # Config chính
└── plugins/
    └── FortniteHits/
        ├── FortniteHits.dll               # Plugin chính
        ├── Config/
        │   └── PluginConfig.cs            # Class config
        ├── Managers/
        │   ├── PlayerManager.cs           # Quản lý player
        │   └── DamageManager.cs           # Quản lý damage/particle
        ├── Utils/
        │   └── Localizer.cs               # Hệ thống đa ngôn ngữ
        ├── data/
        │   └── players.json               # Lưu settings player
        └── langs/
            ├── en.json                    # Tiếng Anh (mặc định)
            ├── vi.json                    # Tiếng Việt
            └── ru.json                    # Tiếng Nga
```

## Kiến trúc Plugin

### 📁 **Config/**
- `PluginConfig.cs`: Class chứa cấu hình plugin

### 📁 **Managers/**
- `PlayerManager.cs`: Quản lý trạng thái, quyền truy cập và settings của player
- `DamageManager.cs`: Xử lý logic damage, particle effects và shotgun mechanics

### 📁 **Utils/**
- `Localizer.cs`: Hệ thống đa ngôn ngữ với prefix tự động

### 📄 **FortniteHits.cs**
- Main plugin file, chứa events và API methods

## Cấu hình

File: `addons/counterstrikesharp/configs/plugins/FortniteHits/FortniteHits.json`

```json
{
  "distance": 40.0,          // Khoảng cách giữa các số damage
  "free_access": true,       // Cho phép tất cả player sử dụng
  "commands": ["fortnite", "fn", "damage"],  // Lệnh toggle
  "ConfigVersion": 1
}
```

### Lưu ý về Commands:
- Nếu để `commands: []` (mảng rỗng) thì không có lệnh nào hoạt động
- Nếu để `commands: [""]` (chứa chuỗi rỗng) thì lệnh đó sẽ bị bỏ qua
- Players sẽ không thể tắt/bật tính năng nếu không có lệnh

## Hệ thống Ngôn ngữ

### Cấu trúc Language Files:
```json
{
  "zFH_Prefix": "{gold}FortniteHits {silver}» ",
  "zFH_Enable": "{green}Damage numbers {default}enabled!",
  "zFH_Disable": "{red}Damage numbers {default}disabled!",
  "zFH_NoAccess": "{red}You don't have access to this feature!"
}
```

### Tính năng Prefix:
- `zFH_Prefix` sẽ tự động được thêm vào trước tất cả các thông báo
- Không cần chỉnh tay cho từng language file
- Hỗ trợ đầy đủ color codes: `{gold}`, `{silver}`, `{red}`, `{green}`, etc.

### Ngôn ngữ hỗ trợ:
- `en` - English (mặc định)
- `vi` - Tiếng Việt  
- `ru` - Русский

Plugin tự động detect ngôn ngữ từ server config của CounterStrikeSharp.

### Thêm ngôn ngữ mới:
1. Tạo file `langs/{language_code}.json`
2. Copy cấu trúc từ `en.json`
3. Dịch các phrase (giữ nguyên key `zFH_*`)
4. Prefix sẽ tự động được áp dụng

## Commands

- `!fortnite` / `!fn` / `!damage` - Bật/tắt hiển thị damage numbers

## API cho Plugin khác

```csharp
// Cấp quyền cho player
plugin.GiveClientAccess(player);

// Thu hồi quyền
plugin.TakeClientAccess(player);

// Kiểm tra quyền truy cập
bool hasAccess = plugin.HasClientAccess(player);

// Kiểm tra trạng thái bật/tắt
bool isEnabled = plugin.IsClientEnabled(player);

// Bật/tắt cho player
plugin.SetClientEnabled(player, true/false);
```

## Tính năng

- ✅ Hiển thị damage numbers cho tất cả vũ khí
- ✅ Tích lũy damage cho shotgun (hiển thị tổng sau 0.1s)
- ✅ Headshot có hiển thị khác biệt
- ✅ Lưu settings cá nhân cho từng player
- ✅ Hệ thống đa ngôn ngữ với prefix tự động
- ✅ Access control system
- ✅ Cross-platform (Windows/Linux)
- ✅ Kiến trúc modular, dễ maintain
- ✅ API đầy đủ cho plugin khác

## Yêu cầu

- CounterStrikeSharp 1.0.180+
- .NET 8.0
- Particle files: `particles/kolka/fortnite_dmg_v2/`

## Build

```bash
dotnet build -c Release
```

File output: `bin/Release/net8.0/FortniteHits.dll`

## Maintainability

Plugin được thiết kế theo mô hình modular:
- **Separation of Concerns**: Mỗi class có trách nhiệm riêng biệt
- **Easy Extension**: Dễ dàng thêm tính năng mới
- **Clean Code**: Code rõ ràng, dễ đọc và maintain
- **Error Handling**: Xử lý lỗi robust
- **Performance**: Tối ưu về performance và memory