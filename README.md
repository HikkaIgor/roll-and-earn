# Roll&Earn — Solana Play-to-Earn RPG

Фэнтези-RPG на блокчейне Solana с on-chain смарт-контрактом (Anchor/Rust) и Unity-клиентом (C#). Игроки создают персонажей (Воин/Разбойник/Маг), бросают кубик D20 в приключениях, зарабатывают токены ROLAND и XP, находят и экипируют NFT-предметы, повышают уровень и получают ежедневные награды.

**Развёрнуто на Devnet:** `GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T`

## Архитектура

```
RollAndEarn/
├── Anchor/                          # Смарт-контракт Solana
│   ├── programs/roll_and_earn/      # Rust-программа (anchor-lang 0.30.1)
│   ├── tests/roll_and_earn.ts       # 21 тест (Mocha + Anchor)
│   ├── scripts/                     # Инициализация игры, вывод PDA
│   ├── metadata/                    # JSON-шаблоны метаданных IPFS
│   └── Anchor.toml                  # Program ID, конфигурация кластера
├── Assets/
│   ├── Scenes/MainScene.unity       # Основная сцена игры
│   ├── Scripts/                     # Код на C#
│   │   ├── Core/                    # SolanaManager, AnchorClient, NFTManager, TokenManager, SoundManager
│   │   ├── GameLogic/               # AdventureManager, CooldownManager, RewardCalculator
│   │   ├── Models/                  # PlayerProfile, RollResult, CharacterData, ItemData
│   │   ├── UI/                      # Экраны, компоненты, навигация, тема
│   │   └── Utils/                   # IPFSLoader, JsonParser, CoroutineUtils
│   ├── Editor/RollAndEarnSetup.cs   # Программный генератор сцены
│   ├── Fonts/                       # Cinzel, MedievalSharp (TTF)
│   └── Resources/RollAndEarn/       # GameConfig, ScriptableObjects, спрайты
```

## Требования

### Смарт-контракт (Anchor)
- **Solana CLI** ≥ 1.17 (`sh -c "$(curl -sSfL https://release.anza.xyz/stable/install)"`)
- **Anchor CLI** ≥ 0.30 (`cargo install --git https://github.com/coral-xyz/anchor anchor-cli --tag v0.30.1`)
- **Node.js** ≥ 18
- **Rust** ≥ 1.75 (`rustup default stable`)

### Unity-клиент
- **Unity** ≥ 2022.3 (шаблон URP)
- **TextMeshPro** (через Package Manager)
- **UniTask** (через Package Manager — `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`)
- **Solana Unity SDK** (`https://github.com/garbles-labs/Solana.Unity-SDK.git`)

## Настройка смарт-контракта

### 1. Установка зависимостей

```bash
cd Anchor
npm install
```

### 2. Сборка

```bash
anchor build
```

Скомпилированный бинарник `.so` находится в `target/deploy/roll_and_earn.so`.

### 3. Запуск тестов (локальный валидатор)

```bash
anchor test
```

Запускает локальный `solana-test-validator`, деплоит программу и выполняет 21 тест, покрывающий все инструкции.

### 4. Деплой на Devnet

```bash
# Настройка Solana CLI на Devnet
solana config set --url devnet

# Пополнить кошелёк (airdrop SOL для оплаты деплоя)
solana airdrop 2

# Деплой
anchor deploy --provider.cluster devnet
```

### 5. Инициализация игры

После деплоя запустите скрипт инициализации on-chain состояния:

```bash
# При необходимости обновите Program ID в скриптах
npx ts-node scripts/init-game.ts
```

Скрипт создаёт:
- **GameState** PDA — глобальное состояние игры (reward mint, treasury)
- **Treasury** PDA — хранит токены ROLAND для наград
- **MintAuthority** PDA — авторитет для минтинга NFT (персонажи + предметы)

## Настройка Unity

### 1. Открыть проект

Откройте папку `RollAndEarn` в Unity Hub (Unity 2022.3+ с URP).

### 2. Установить зависимости

Через **Window > Package Manager > Add package from git URL**:
- `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- `https://github.com/garbles-labs/Solana.Unity-SDK.git`

Убедитесь, что **TextMeshPro** установлен через Package Manager.

### 3. Генерация шрифтов

Меню: **RollAndEarn > Generate Font Assets**

Создаёт TMP SDF-ассеты шрифтов из TTF-файлов в `Assets/Fonts/`.

### 4. Настройка GameConfig

Откройте `Assets/Resources/RollAndEarn/GameConfig.asset` в Inspector:

| Поле | Значение |
|------|----------|
| RPC Endpoint | `https://api.devnet.solana.com` |
| Roland Mint Address | Адрес минта ROLAND |
| Program ID | `GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T` |
| IPFS Gateway | `https://ipfs.io/ipfs/` |
| NFT Metadata URIs | IPFS URI (предзаполнены) |

### 5. Настройка сцены

Меню: **RollAndEarn > Setup Everything**

Программно генерирует весь UI (6 экранов, нижняя навигация, все компоненты) в активной сцене. Ручная настройка префабов не требуется.

### 6. Запуск

Нажмите **Play** в Unity Editor. Игра откроется на экране подключения кошелька.

## Игровой процесс

1. **Подключить кошелёк** — Встроенный кошелёк (editor) или WebGL Phantom/Solflare
2. **Создать персонажа** — Выбрать Воина, Разбойника или Мага; ввести имя
3. **Получить аирдроп** — Разовый кран на 200 ROLAND
4. **Отправиться в приключение**:
   - Заколдованный Лес (бесплатно, кд 5 мин)
   - Тёмное Подземелье (10 ROLAND, кд 15 мин)
   - Логово Дракона (50 ROLAND, кд 60 мин)
5. **Бросить D20** — Анимированный кубик, награды по тиру (токены + XP + спецпредметы)
6. **Экипировать предметы** — Оружие и броня дают бонус к броскам D20
7. **Повысить уровень** — Сжечь токены + XP для повышения статов
8. **Ежедневные награды** — 50-120 ROLAND с бонусами за серию

## Инструкции смарт-контракта (12)

| Инструкция | Описание |
|------------|----------|
| `init_game` | Инициализация GameState + MintAuthority PDA |
| `init_treasury` | Инициализация Treasury PDA + ATA |
| `create_character` | Создать профиль, минтить NFT персонажа, установить статы класса |
| `roll_action` | Бросок D20, награды, кулдаун, XP |
| `claim_item` | Минтить NFT предмета из неполученных спецпредметов |
| `equip_item` | Экипировать оружие/броню, вычислить бонус |
| `unequip_item` | Снять оружие/броню |
| `level_up` | Сжечь токены + XP для повышения уровня |
| `request_airdrop` | Разовый кран 200 ROLAND |
| `claim_daily_reward` | Ежедневные 50-120 ROLAND с серией |
| `init_profile` | Создать пустой профиль PDA через CPI |
| `recreate_profile` | Перезаписать профиль для существующего минта |

## Стек технологий

- **Смарт-контракт:** Rust, Anchor Framework 0.30.1, Solana (devnet)
- **Unity-клиент:** C#, Unity 2022.3 URP, TextMeshPro, UniTask
- **Блокчейн:** Solana Web3.js, Solana Unity SDK, spl-token
- **Метаданные NFT:** IPFS (Pinata), on-chain URI-ссылки
- **Аудио:** Процедурный синтез (без внешних аудиофайлов)

## Лицензия

MIT
