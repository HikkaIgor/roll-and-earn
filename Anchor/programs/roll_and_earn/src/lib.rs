use anchor_lang::prelude::*;
use anchor_lang::solana_program::hash::hash as sol_hash;
use anchor_lang::Discriminator;
use anchor_spl::associated_token::AssociatedToken;
use anchor_spl::token::{self, Burn, Mint, MintTo, Token, TokenAccount, Transfer};

declare_id!("GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");

const GAME_STATE_SEED: &[u8] = b"game_state";
const TREASURY_SEED: &[u8] = b"treasury";
const MINT_AUTHORITY_SEED: &[u8] = b"mint_authority";
const PLAYER_PROFILE_SEED: &[u8] = b"player_profile";
const CHARACTER_MINT_SEED: &[u8] = b"character_mint";
const ITEM_MINT_SEED: &[u8] = b"item_mint";
const DAILY_REWARD_BASE: u64 = 50_000_000_000;
const DAILY_REWARD_STREAK_BONUS: u64 = 10_000_000_000;
const DAILY_REWARD_MAX: u64 = 120_000_000_000;
const DAILY_COOLDOWN_SECONDS: i64 = 86_400;
const DAILY_STREAK_EXPIRY_SECONDS: i64 = 172_800;
const FAUCET_AMOUNT: u64 = 200_000_000_000;

#[program]
pub mod roll_and_earn {
    use super::*;

    pub fn init_game(ctx: Context<InitGame>) -> Result<()> {
        let game_state = &mut ctx.accounts.game_state;
        game_state.authority = ctx.accounts.authority.key();
        game_state.reward_mint = ctx.accounts.reward_mint.key();
        game_state.treasury = Pubkey::default();
        game_state.total_rolls = 0;
        game_state.total_tokens_distributed = 0;
        game_state.bump = ctx.bumps.game_state;
        ctx.accounts.mint_authority.bump = ctx.bumps.mint_authority;
        Ok(())
    }

    pub fn init_treasury(ctx: Context<InitTreasury>) -> Result<()> {
        let game_state = &mut ctx.accounts.game_state;
        game_state.treasury = ctx.accounts.treasury.key();
        ctx.accounts.treasury.bump = ctx.bumps.treasury;
        Ok(())
    }

    pub fn create_character(
        ctx: Context<CreateCharacter>,
        _name: String,
        _symbol: String,
        _uri: String,
        class: u8,
    ) -> Result<()> {
        require!(class <= 2, ErrorCode::InvalidClass);
        require!(_name.len() <= 32, ErrorCode::NameTooLong);
        require!(_symbol.len() <= 10, ErrorCode::SymbolTooLong);

        let ma_bump = ctx.accounts.mint_authority.bump;
        let signer_seeds: &[&[&[u8]]] = &[&[MINT_AUTHORITY_SEED, &[ma_bump]]];

        token::mint_to(
            CpiContext::new_with_signer(
                ctx.accounts.token_program.to_account_info(),
                MintTo {
                    mint: ctx.accounts.character_mint.to_account_info(),
                    to: ctx.accounts.player_character_ata.to_account_info(),
                    authority: ctx.accounts.mint_authority.to_account_info(),
                },
                signer_seeds,
            ),
            1,
        )?;

        let profile = &mut ctx.accounts.player_profile;
        profile.owner = ctx.accounts.player.key();
        profile.character_mint = ctx.accounts.character_mint.key();
        profile.level = 1;
        profile.xp = 0;
        profile.last_action_ts = 0;
        profile.cooldown_expiries = [0; 3];
        profile.equipped_weapon = Pubkey::default();
        profile.equipped_armor = Pubkey::default();
        profile.class = class;
        profile.items_minted = 0;
        profile.unclaimed_specials = 0;
        profile.last_daily_claim_ts = 0;
        profile.daily_streak = 0;
        profile.weapon_bonus = 0;
        profile.armor_bonus = 0;
        profile.airdrop_claimed = false;
        profile.bump = ctx.bumps.player_profile;

        match class {
            0 => {
                profile.strength = 10;
                profile.agility = 6;
                profile.intelligence = 4;
                profile.luck = 5;
            }
            1 => {
                profile.strength = 5;
                profile.agility = 10;
                profile.intelligence = 6;
                profile.luck = 8;
            }
            2 => {
                profile.strength = 4;
                profile.agility = 5;
                profile.intelligence = 10;
                profile.luck = 6;
            }
            _ => return Err(ErrorCode::InvalidClass.into()),
        }

        emit!(CharacterCreated {
            player: ctx.accounts.player.key(),
            character_mint: ctx.accounts.character_mint.key(),
            class,
        });
        Ok(())
    }

    pub fn roll_action(ctx: Context<RollAction>, adventure_type: u8) -> Result<()> {
        require!(adventure_type <= 2, ErrorCode::InvalidAdventure);
        let profile = &mut ctx.accounts.player_profile;
        let clock = &ctx.accounts.clock;
        let game_state = &mut ctx.accounts.game_state;
        require!(
            clock.unix_timestamp >= profile.cooldown_expiries[adventure_type as usize],
            ErrorCode::CooldownActive
        );

        let adventure = AdventureConfig::get(adventure_type)?;

        if adventure.cost > 0 {
            token::transfer(
                CpiContext::new(
                    ctx.accounts.token_program.to_account_info(),
                    Transfer {
                        from: ctx.accounts.player_token_account.to_account_info(),
                        to: ctx.accounts.treasury_token_account.to_account_info(),
                        authority: ctx.accounts.player.to_account_info(),
                    },
                ),
                adventure.cost,
            )?;
        }

        let mut seed_vec = Vec::new();
        seed_vec.extend_from_slice(ctx.accounts.player.key().as_ref());
        seed_vec.extend_from_slice(&clock.unix_timestamp.to_le_bytes());
        seed_vec.extend_from_slice(&game_state.total_rolls.to_le_bytes());
        let hash_result = sol_hash(&seed_vec);
        let base_roll =
            ((u64::from_le_bytes(hash_result.to_bytes()[0..8].try_into().unwrap()) % 20) + 1) as u8;

        let effective_roll = (base_roll as u16
            + profile.weapon_bonus as u16
            + profile.armor_bonus as u16)
            .min(25) as u8;

        let reward = adventure.calculate_reward(effective_roll);

        if reward.token_amount > 0 {
            let t_bump = ctx.accounts.treasury.bump;
            let signer_seeds: &[&[&[u8]]] = &[&[TREASURY_SEED, &[t_bump]]];
            token::transfer(
                CpiContext::new_with_signer(
                    ctx.accounts.token_program.to_account_info(),
                    Transfer {
                        from: ctx.accounts.treasury_token_account.to_account_info(),
                        to: ctx.accounts.player_token_account.to_account_info(),
                        authority: ctx.accounts.treasury.to_account_info(),
                    },
                    signer_seeds,
                ),
                reward.token_amount,
            )?;
        }

        profile.last_action_ts = clock.unix_timestamp;
        profile.cooldown_expiries[adventure_type as usize] = clock.unix_timestamp + adventure.cooldown_seconds as i64;
        profile.xp = profile.xp.saturating_add(reward.xp);
        if reward.is_special {
            profile.unclaimed_specials = profile.unclaimed_specials.saturating_add(1);
        }
        game_state.total_rolls = game_state.total_rolls.saturating_add(1);
        game_state.total_tokens_distributed = game_state
            .total_tokens_distributed
            .saturating_add(reward.token_amount);

        emit!(RollCompleted {
            player: ctx.accounts.player.key(),
            base_roll,
            effective_roll,
            adventure_type,
            reward_amount: reward.token_amount,
            xp_gained: reward.xp,
            is_special: reward.is_special,
            timestamp: clock.unix_timestamp,
        });
        Ok(())
    }

    pub fn claim_item(
        ctx: Context<ClaimItem>,
        name: String,
        _symbol: String,
        _uri: String,
    ) -> Result<()> {
        let profile = &mut ctx.accounts.player_profile;
        require!(
            profile.unclaimed_specials > 0,
            ErrorCode::NotEligibleForItem
        );
        require!(name.len() <= 32, ErrorCode::NameTooLong);
        let ma_bump = ctx.accounts.mint_authority.bump;
        let signer_seeds: &[&[&[u8]]] = &[&[MINT_AUTHORITY_SEED, &[ma_bump]]];

        token::mint_to(
            CpiContext::new_with_signer(
                ctx.accounts.token_program.to_account_info(),
                MintTo {
                    mint: ctx.accounts.item_mint.to_account_info(),
                    to: ctx.accounts.player_item_ata.to_account_info(),
                    authority: ctx.accounts.mint_authority.to_account_info(),
                },
                signer_seeds,
            ),
            1,
        )?;

        profile.unclaimed_specials = profile.unclaimed_specials.saturating_sub(1);
        profile.items_minted = profile.items_minted.saturating_add(1);

        emit!(ItemClaimed {
            player: ctx.accounts.player.key(),
            item_mint: ctx.accounts.item_mint.key(),
            items_minted: profile.items_minted,
        });
        Ok(())
    }

    pub fn equip_item(ctx: Context<EquipItem>, item_type: u8) -> Result<()> {
        require!(item_type <= 1, ErrorCode::InvalidItemType);
        let profile = &mut ctx.accounts.player_profile;
        let item_hash = sol_hash(ctx.accounts.item_mint.key().as_ref());
        let bonus = (item_hash.to_bytes()[0] % 5) + 1;
        match item_type {
            0 => {
                profile.equipped_weapon = ctx.accounts.item_mint.key();
                profile.weapon_bonus = bonus;
            }
            1 => {
                profile.equipped_armor = ctx.accounts.item_mint.key();
                profile.armor_bonus = bonus;
            }
            _ => unreachable!(),
        }
        emit!(ItemEquipped {
            player: ctx.accounts.player.key(),
            item_mint: ctx.accounts.item_mint.key(),
            item_type,
            bonus,
        });
        Ok(())
    }

    pub fn unequip_item(ctx: Context<UnequipItem>, item_type: u8) -> Result<()> {
        require!(item_type <= 1, ErrorCode::InvalidItemType);
        let profile = &mut ctx.accounts.player_profile;
        match item_type {
            0 => {
                profile.equipped_weapon = Pubkey::default();
                profile.weapon_bonus = 0;
            }
            1 => {
                profile.equipped_armor = Pubkey::default();
                profile.armor_bonus = 0;
            }
            _ => unreachable!(),
        }
        emit!(ItemUnequipped {
            player: ctx.accounts.player.key(),
            item_type,
        });
        Ok(())
    }

    pub fn request_airdrop(ctx: Context<RequestAirdrop>) -> Result<()> {
        let profile = &mut ctx.accounts.player_profile;
        require!(!profile.airdrop_claimed, ErrorCode::AirdropAlreadyClaimed);

        let t_bump = ctx.accounts.treasury.bump;
        let signer_seeds: &[&[&[u8]]] = &[&[TREASURY_SEED, &[t_bump]]];

        token::transfer(
            CpiContext::new_with_signer(
                ctx.accounts.token_program.to_account_info(),
                Transfer {
                    from: ctx.accounts.treasury_token_account.to_account_info(),
                    to: ctx.accounts.player_token_account.to_account_info(),
                    authority: ctx.accounts.treasury.to_account_info(),
                },
                signer_seeds,
            ),
            FAUCET_AMOUNT,
        )?;

        profile.airdrop_claimed = true;

        emit!(AirdropClaimed {
            player: ctx.accounts.player.key(),
            amount: FAUCET_AMOUNT,
        });
        Ok(())
    }

    pub fn force_close_profile(ctx: Context<ForceCloseProfile>) -> Result<()> {
        let profile_info = ctx.accounts.player_profile.to_account_info();
        **ctx.accounts.player.try_borrow_mut_lamports()? += **profile_info.try_borrow_lamports()?;
        **profile_info.try_borrow_mut_lamports()? = 0;
        profile_info.realloc(0, false)?;

        for acc in ctx.remaining_accounts.iter() {
            **ctx.accounts.player.try_borrow_mut_lamports()? += **acc.try_borrow_lamports()?;
            **acc.try_borrow_mut_lamports()? = 0;
            acc.realloc(0, false)?;
        }
        Ok(())
    }

    pub fn init_profile(ctx: Context<InitProfile>) -> Result<()> {
        let profile_info = ctx.accounts.player_profile.to_account_info();
        let player_info = ctx.accounts.player.to_account_info();

        if profile_info.data_len() == 0 && profile_info.lamports() == 0 {
            let rent = Rent::get()?;
            let lamports = rent.minimum_balance(PlayerProfile::LEN);
            let bump = ctx.bumps.player_profile;
            let seeds: &[&[u8]] = &[
                PLAYER_PROFILE_SEED,
                player_info.key.as_ref(),
                &[bump],
            ];

            let ix = anchor_lang::solana_program::system_instruction::create_account(
                player_info.key,
                profile_info.key,
                lamports,
                PlayerProfile::LEN as u64,
                ctx.program_id,
            );

            anchor_lang::solana_program::program::invoke_signed(
                &ix,
                &[player_info, profile_info],
                &[seeds],
            )?;
        }

        Ok(())
    }

    pub fn recreate_profile(
        ctx: Context<RecreateProfile>,
        class: u8,
    ) -> Result<()> {
        require!(class <= 2, ErrorCode::InvalidClass);

        let profile_info = ctx.accounts.player_profile.to_account_info();
        msg!("profile data_len={} lamports={} owner={}", profile_info.data_len(), profile_info.lamports(), profile_info.owner);

        let (strength, agility, intelligence, luck) = match class {
            0 => (10u8, 6u8, 4u8, 5u8),
            1 => (5u8, 10u8, 6u8, 8u8),
            2 => (4u8, 5u8, 10u8, 6u8),
            _ => return Err(ErrorCode::InvalidClass.into()),
        };

        let profile = PlayerProfile {
            owner: ctx.accounts.player.key(),
            character_mint: ctx.accounts.character_mint.key(),
            level: 1,
            xp: 0,
            last_action_ts: 0,
            cooldown_expiries: [0; 3],
            equipped_weapon: Pubkey::default(),
            equipped_armor: Pubkey::default(),
            class,
            strength,
            agility,
            intelligence,
            luck,
            items_minted: 0,
            unclaimed_specials: 0,
            last_daily_claim_ts: 0,
            daily_streak: 0,
            weapon_bonus: 0,
            armor_bonus: 0,
            airdrop_claimed: false,
            bump: ctx.bumps.player_profile,
        };

        let mut data = profile_info.try_borrow_mut_data()?;
        msg!("buffer len={}", data.len());
        data[..8].copy_from_slice(&PlayerProfile::DISCRIMINATOR);
        let serialized = profile.try_to_vec()?;
        msg!("serialized len={}", serialized.len());
        data[8..8 + serialized.len()].copy_from_slice(&serialized);

        emit!(CharacterCreated {
            player: ctx.accounts.player.key(),
            character_mint: ctx.accounts.character_mint.key(),
            class,
        });
        Ok(())
    }

    pub fn level_up(ctx: Context<LevelUp>) -> Result<()> {
        let profile = &mut ctx.accounts.player_profile;
        let xp_needed = (profile.level as u32) * 100;
        let cost = (profile.level as u64) * 50_000_000_000;
        require!(profile.xp >= xp_needed, ErrorCode::InsufficientXP);

        token::burn(
            CpiContext::new(
                ctx.accounts.token_program.to_account_info(),
                Burn {
                    mint: ctx.accounts.reward_mint.to_account_info(),
                    from: ctx.accounts.player_token_account.to_account_info(),
                    authority: ctx.accounts.player.to_account_info(),
                },
            ),
            cost,
        )?;

        profile.level = profile.level.saturating_add(1);
        profile.xp = profile.xp.saturating_sub(xp_needed);
        match profile.class {
            0 => {
                profile.strength = profile.strength.saturating_add(2);
                profile.agility = profile.agility.saturating_add(1);
            }
            1 => {
                profile.agility = profile.agility.saturating_add(2);
                profile.luck = profile.luck.saturating_add(1);
            }
            2 => {
                profile.intelligence = profile.intelligence.saturating_add(2);
                profile.luck = profile.luck.saturating_add(1);
            }
            _ => {}
        }
        emit!(LevelUpEvent {
            player: ctx.accounts.player.key(),
            new_level: profile.level,
            strength: profile.strength,
            agility: profile.agility,
            intelligence: profile.intelligence,
            luck: profile.luck,
        });
        Ok(())
    }

    pub fn claim_daily_reward(ctx: Context<ClaimDailyReward>) -> Result<()> {
        let profile = &mut ctx.accounts.player_profile;
        let clock = &ctx.accounts.clock;
        let now = clock.unix_timestamp;

        require!(
            profile.last_daily_claim_ts == 0
                || now >= profile.last_daily_claim_ts + DAILY_COOLDOWN_SECONDS,
            ErrorCode::DailyCooldownActive
        );

        if profile.last_daily_claim_ts > 0
            && now > profile.last_daily_claim_ts + DAILY_STREAK_EXPIRY_SECONDS
        {
            profile.daily_streak = 0;
        }

        let streak_bonus = (profile.daily_streak as u64) * DAILY_REWARD_STREAK_BONUS;
        let reward = (DAILY_REWARD_BASE + streak_bonus).min(DAILY_REWARD_MAX);

        let t_bump = ctx.accounts.treasury.bump;
        let signer_seeds: &[&[&[u8]]] = &[&[TREASURY_SEED, &[t_bump]]];

        token::transfer(
            CpiContext::new_with_signer(
                ctx.accounts.token_program.to_account_info(),
                Transfer {
                    from: ctx.accounts.treasury_token_account.to_account_info(),
                    to: ctx.accounts.player_token_account.to_account_info(),
                    authority: ctx.accounts.treasury.to_account_info(),
                },
                signer_seeds,
            ),
            reward,
        )?;

        profile.last_daily_claim_ts = now;
        profile.daily_streak = profile.daily_streak.saturating_add(1);

        emit!(DailyRewardClaimed {
            player: ctx.accounts.player.key(),
            amount: reward,
            streak: profile.daily_streak,
        });
        Ok(())
    }
}

#[account]
pub struct GameState {
    pub authority: Pubkey,
    pub reward_mint: Pubkey,
    pub treasury: Pubkey,
    pub total_rolls: u64,
    pub total_tokens_distributed: u64,
    pub bump: u8,
}
impl GameState {
    pub const LEN: usize = 8 + 32 + 32 + 32 + 8 + 8 + 1;
}

#[account]
pub struct PlayerProfile {
    pub owner: Pubkey,
    pub character_mint: Pubkey,
    pub level: u8,
    pub xp: u32,
    pub last_action_ts: i64,
    pub cooldown_expiries: [i64; 3],
    pub equipped_weapon: Pubkey,
    pub equipped_armor: Pubkey,
    pub class: u8,
    pub strength: u8,
    pub agility: u8,
    pub intelligence: u8,
    pub luck: u8,
    pub items_minted: u8,
    pub unclaimed_specials: u8,
    pub last_daily_claim_ts: i64,
    pub daily_streak: u16,
    pub weapon_bonus: u8,
    pub armor_bonus: u8,
    pub airdrop_claimed: bool,
    pub bump: u8,
}
impl PlayerProfile {
    pub const LEN: usize = 8
        + 32 + 32
        + 1 + 4
        + 8 + 24
        + 32 + 32
        + 1 + 1 + 1 + 1
        + 1 + 1 + 1
        + 8 + 2
        + 1 + 1 + 1
        + 1;
}

#[account]
pub struct Treasury {
    pub bump: u8,
}
impl Treasury {
    pub const LEN: usize = 8 + 1;
}

#[account]
pub struct MintAuthority {
    pub bump: u8,
}
impl MintAuthority {
    pub const LEN: usize = 8 + 1;
}

struct AdventureConfig {
    cost: u64,
    cooldown_seconds: u32,
    mid_start: u8,
    high_start: u8,
    special_start: u8,
    low_reward: u64,
    mid_reward: u64,
    high_reward: u64,
    low_xp: u32,
    mid_xp: u32,
    high_xp: u32,
    special_xp: u32,
}

struct RewardResult {
    token_amount: u64,
    xp: u32,
    is_special: bool,
}

impl AdventureConfig {
    fn get(atype: u8) -> Result<Self> {
        match atype {
            0 => Ok(AdventureConfig {
                cost: 0,
                cooldown_seconds: 300,
                mid_start: 6,
                high_start: 11,
                special_start: 20,
                low_reward: 5_000_000_000,
                mid_reward: 15_000_000_000,
                high_reward: 30_000_000_000,
                low_xp: 5,
                mid_xp: 15,
                high_xp: 25,
                special_xp: 35,
            }),
            1 => Ok(AdventureConfig {
                cost: 10_000_000_000,
                cooldown_seconds: 900,
                mid_start: 5,
                high_start: 9,
                special_start: 18,
                low_reward: 5_000_000_000,
                mid_reward: 25_000_000_000,
                high_reward: 60_000_000_000,
                low_xp: 10,
                mid_xp: 25,
                high_xp: 50,
                special_xp: 75,
            }),
            2 => Ok(AdventureConfig {
                cost: 50_000_000_000,
                cooldown_seconds: 3600,
                mid_start: 4,
                high_start: 8,
                special_start: 16,
                low_reward: 10_000_000_000,
                mid_reward: 50_000_000_000,
                high_reward: 150_000_000_000,
                low_xp: 20,
                mid_xp: 50,
                high_xp: 100,
                special_xp: 150,
            }),
            _ => Err(ErrorCode::InvalidAdventure.into()),
        }
    }

    fn calculate_reward(&self, roll: u8) -> RewardResult {
        if roll >= self.special_start {
            RewardResult {
                token_amount: self.high_reward,
                xp: self.special_xp,
                is_special: true,
            }
        } else if roll >= self.high_start {
            RewardResult {
                token_amount: self.high_reward,
                xp: self.high_xp,
                is_special: false,
            }
        } else if roll >= self.mid_start {
            RewardResult {
                token_amount: self.mid_reward,
                xp: self.mid_xp,
                is_special: false,
            }
        } else {
            RewardResult {
                token_amount: self.low_reward,
                xp: self.low_xp,
                is_special: false,
            }
        }
    }
}

#[derive(Accounts)]
pub struct InitGame<'info> {
    #[account(mut)]
    pub authority: Signer<'info>,
    #[account(init, payer = authority, space = GameState::LEN, seeds = [GAME_STATE_SEED], bump)]
    pub game_state: Box<Account<'info, GameState>>,
    #[account(init, payer = authority, space = MintAuthority::LEN, seeds = [MINT_AUTHORITY_SEED], bump)]
    pub mint_authority: Box<Account<'info, MintAuthority>>,
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct InitTreasury<'info> {
    #[account(mut)]
    pub authority: Signer<'info>,
    #[account(mut, constraint = game_state.authority == authority.key())]
    pub game_state: Box<Account<'info, GameState>>,
    #[account(init, payer = authority, space = Treasury::LEN, seeds = [TREASURY_SEED], bump)]
    pub treasury: Box<Account<'info, Treasury>>,
    /// CHECK: Treasury ATA created manually via CPI
    #[account(mut)]
    pub treasury_token_account: UncheckedAccount<'info>,
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct CreateCharacter<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(init, payer = player, space = PlayerProfile::LEN, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump)]
    pub player_profile: Box<Account<'info, PlayerProfile>>,
    #[account(init, payer = player, mint::decimals = 0, mint::authority = mint_authority, seeds = [CHARACTER_MINT_SEED, player.key().as_ref()], bump)]
    pub character_mint: Box<Account<'info, Mint>>,
    #[account(init, payer = player, associated_token::mint = character_mint, associated_token::authority = player)]
    pub player_character_ata: Box<Account<'info, TokenAccount>>,
    #[account(seeds = [MINT_AUTHORITY_SEED], bump = mint_authority.bump)]
    pub mint_authority: Box<Account<'info, MintAuthority>>,
    pub game_state: Box<Account<'info, GameState>>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct RollAction<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    #[account(mut)]
    pub game_state: Account<'info, GameState>,
    #[account(seeds = [TREASURY_SEED], bump = treasury.bump)]
    pub treasury: Account<'info, Treasury>,
    #[account(mut, constraint = treasury_token_account.mint == reward_mint.key(), constraint = treasury_token_account.owner == treasury.key())]
    pub treasury_token_account: Account<'info, TokenAccount>,
    #[account(mut, constraint = player_token_account.mint == reward_mint.key(), constraint = player_token_account.owner == player.key())]
    pub player_token_account: Account<'info, TokenAccount>,
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub clock: Sysvar<'info, Clock>,
}

#[derive(Accounts)]
pub struct ClaimItem<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    #[account(init, payer = player, mint::decimals = 0, mint::authority = mint_authority, seeds = [ITEM_MINT_SEED, player.key().as_ref(), &player_profile.items_minted.to_le_bytes()], bump)]
    pub item_mint: Account<'info, Mint>,
    #[account(init, payer = player, associated_token::mint = item_mint, associated_token::authority = player)]
    pub player_item_ata: Account<'info, TokenAccount>,
    #[account(seeds = [MINT_AUTHORITY_SEED], bump = mint_authority.bump)]
    pub mint_authority: Account<'info, MintAuthority>,
    pub game_state: Account<'info, GameState>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct EquipItem<'info> {
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    pub item_mint: Account<'info, Mint>,
    #[account(constraint = item_token_account.owner == player.key(), constraint = item_token_account.amount >= 1)]
    pub item_token_account: Account<'info, TokenAccount>,
    pub game_state: Account<'info, GameState>,
}

#[derive(Accounts)]
pub struct UnequipItem<'info> {
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    pub game_state: Account<'info, GameState>,
}

#[derive(Accounts)]
pub struct RequestAirdrop<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    #[account(seeds = [TREASURY_SEED], bump = treasury.bump)]
    pub treasury: Account<'info, Treasury>,
    #[account(mut, constraint = treasury_token_account.mint == reward_mint.key(), constraint = treasury_token_account.owner == treasury.key())]
    pub treasury_token_account: Account<'info, TokenAccount>,
    #[account(mut, constraint = player_token_account.mint == reward_mint.key(), constraint = player_token_account.owner == player.key())]
    pub player_token_account: Account<'info, TokenAccount>,
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
}

#[derive(Accounts)]
pub struct LevelUp<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    pub game_state: Account<'info, GameState>,
    #[account(mut, constraint = player_token_account.mint == reward_mint.key(), constraint = player_token_account.owner == player.key())]
    pub player_token_account: Account<'info, TokenAccount>,
    #[account(mut)]
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
}

#[derive(Accounts)]
pub struct ClaimDailyReward<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump = player_profile.bump)]
    pub player_profile: Account<'info, PlayerProfile>,
    #[account(seeds = [TREASURY_SEED], bump = treasury.bump)]
    pub treasury: Account<'info, Treasury>,
    #[account(mut, constraint = treasury_token_account.mint == reward_mint.key(), constraint = treasury_token_account.owner == treasury.key())]
    pub treasury_token_account: Account<'info, TokenAccount>,
    #[account(mut, constraint = player_token_account.mint == reward_mint.key(), constraint = player_token_account.owner == player.key())]
    pub player_token_account: Account<'info, TokenAccount>,
    pub reward_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub clock: Sysvar<'info, Clock>,
}

#[derive(Accounts)]
pub struct ForceCloseProfile<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    /// CHECK: Force closing old profile, data may not match current struct
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump)]
    pub player_profile: UncheckedAccount<'info>,
}

#[derive(Accounts)]
pub struct InitProfile<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    /// CHECK: Profile PDA created via manual CPI
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump)]
    pub player_profile: UncheckedAccount<'info>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct RecreateProfile<'info> {
    #[account(mut)]
    pub player: Signer<'info>,
    /// CHECK: Profile PDA - manually writing data
    #[account(mut, seeds = [PLAYER_PROFILE_SEED, player.key().as_ref()], bump)]
    pub player_profile: UncheckedAccount<'info>,
    /// CHECK: Existing character mint PDA
    #[account(seeds = [CHARACTER_MINT_SEED, player.key().as_ref()], bump)]
    pub character_mint: UncheckedAccount<'info>,
}

#[error_code]
pub enum ErrorCode {
    #[msg("Invalid character class")]
    InvalidClass,
    #[msg("Name too long")]
    NameTooLong,
    #[msg("Symbol too long")]
    SymbolTooLong,
    #[msg("Invalid adventure type")]
    InvalidAdventure,
    #[msg("Cooldown active")]
    CooldownActive,
    #[msg("Not eligible for item")]
    NotEligibleForItem,
    #[msg("Invalid item type")]
    InvalidItemType,
    #[msg("Insufficient XP")]
    InsufficientXP,
    #[msg("Daily reward cooldown active")]
    DailyCooldownActive,
    #[msg("Invalid metadata PDA")]
    InvalidMetadataPda,
    #[msg("Airdrop already claimed")]
    AirdropAlreadyClaimed,
}

#[event]
pub struct CharacterCreated {
    pub player: Pubkey,
    pub character_mint: Pubkey,
    pub class: u8,
}

#[event]
pub struct RollCompleted {
    pub player: Pubkey,
    pub base_roll: u8,
    pub effective_roll: u8,
    pub adventure_type: u8,
    pub reward_amount: u64,
    pub xp_gained: u32,
    pub is_special: bool,
    pub timestamp: i64,
}

#[event]
pub struct ItemClaimed {
    pub player: Pubkey,
    pub item_mint: Pubkey,
    pub items_minted: u8,
}

#[event]
pub struct ItemEquipped {
    pub player: Pubkey,
    pub item_mint: Pubkey,
    pub item_type: u8,
    pub bonus: u8,
}

#[event]
pub struct ItemUnequipped {
    pub player: Pubkey,
    pub item_type: u8,
}

#[event]
pub struct AirdropClaimed {
    pub player: Pubkey,
    pub amount: u64,
}

#[event]
pub struct LevelUpEvent {
    pub player: Pubkey,
    pub new_level: u8,
    pub strength: u8,
    pub agility: u8,
    pub intelligence: u8,
    pub luck: u8,
}

#[event]
pub struct DailyRewardClaimed {
    pub player: Pubkey,
    pub amount: u64,
    pub streak: u16,
}
