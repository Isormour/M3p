import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const toolDir = dirname(fileURLToPath(import.meta.url));
const root = resolve(toolDir, '../..');
const audioRoot = join(root, 'Assets', '_Project', 'Audio', 'SFX');
const docPath = join(root, 'Docs', 'M3p_miejsca_na_dzwiek.md');
const cliPath = join(toolDir, 'node_modules', '@elevenlabs', 'cli', 'bin', 'cli.js');
const keyLine = readFileSync(join(toolDir, '.env'), 'utf8').split(/\r?\n/).find(line => line.startsWith('ELEVENLABS_API_KEY='));
const apiKey = keyLine?.slice('ELEVENLABS_API_KEY='.length).trim();
if (!apiKey) throw new Error('Brak ELEVENLABS_API_KEY w Tools/ElevenLabs/.env');

const c = (name, description, seconds = 1.5, seed = null) => ({ name, description, seconds, seed });
const eventGroups = [
  { dir: 'MenuMap', rows: [
    [c('menu_confirm', 'A crisp, understated click confirming a dark fantasy game menu selection, with a tiny wooden and metallic tail', 1.0)],
    [c('scene_transition', 'A brief dark fantasy scene transition, a soft low whoosh resolving into a clear arrival accent', 1.5)],
    [c('map_node_select', 'A map destination node is selected, a precise parchment tap with a faint magical glint', 1.0)],
    [c('route_preview', 'A travel route preview panel unfolds, soft parchment rustle and delicate magical chime', 1.2)],
    [c('route_confirm', 'A travel route is confirmed, firm parchment stamp with a bright final click', 1.0), c('route_cancel', 'A travel route is cancelled, short soft reverse parchment swipe and muted click', 1.0)],
    [c('map_token_step', 'One small leather boot step on an old stone map board, clean and light, suitable for rhythmic token movement', 1.0)],
    [c('map_arrival', 'Arrival at a destination on a dark fantasy map, a compact resolving chime with a soft grounded tap', 1.3)],
    [c('encounter_normal', 'A normal combat encounter begins, short ominous drumlike impact and steel whisper, no musical rhythm', 1.5), c('encounter_elite', 'An elite combat encounter begins, stronger ominous iron impact with a tense rising shimmer', 1.8), c('encounter_boss', 'A boss encounter begins, imposing low dark fantasy impact with a brief rising metallic tension and final hit', 2.0)],
    [c('card_shop_open', 'A fantasy card shop opens, a light wooden door creak, coins softly clink and a welcoming chime', 1.7)],
    [c('forge_open', 'A short dark fantasy forge opening UI sound: a heavy iron latch clicks, followed by one warm, resonant anvil strike and a subtle magical ember shimmer. Crisp attack, quick decay', 1.5, 'forge_open_test.mp3')],
    [c('chest_open', 'An old treasure chest latch snaps open and the heavy wooden lid rises with a brief creak', 1.8)],
    [c('chest_reward', 'A fantasy treasure reward is collected, a compact handful of coins and a satisfying bright magical glint', 1.5)],
    [c('chest_empty', 'An empty chest is acknowledged, small hollow wooden tap and neutral UI click', 1.0)],
    [c('panel_open', 'A dark fantasy interface panel opens with a soft parchment slide and tiny wooden click', 1.0), c('panel_close', 'A dark fantasy interface panel closes with a short parchment fold and muted click', 1.0)],
  ]},
  { dir: 'Cards', rows: [
    [c('battle_start', 'A fantasy battle board activates, decisive low impact, cards settle and magical energy flickers', 1.8)],
    [c('player_turn_start', 'The player turn begins and control returns, a concise hopeful metallic and magical chime', 1.2)],
    [c('hand_draw', 'Several fantasy playing cards are quickly drawn into a hand, layered paper shuffles and a clean finish', 1.5)],
    [c('single_card_draw', 'One thick fantasy playing card is drawn from a deck, a dry crisp paper slide', 1.0)],
    [c('card_select', 'A fantasy card is selected from a hand, light paper flick with a precise UI click', 1.0)],
    [c('card_deselect', 'A selected card returns to the hand, soft reverse paper flick and quiet click', 1.0)],
    [c('target_tile_select', 'A valid battle board tile is targeted by a card, short precise glassy tile tick', 1.0)],
    [c('target_selection_complete', 'The last required battle board target is selected, small satisfying double chime', 1.0)],
    [c('card_choice_open', 'A card choice overlay opens for color or gravity direction, a restrained magical UI shimmer', 1.2)],
    [c('card_choice_confirm', 'An extra card choice is confirmed, firm short magical click', 1.0), c('card_choice_cancel', 'An extra card choice is cancelled, soft descending UI tick', 1.0)],
    [c('card_queue_add', 'A selected action card locks into the queue, clear satisfying wooden and metallic click', 1.0)],
    [c('card_queue_undo', 'The last queued action card is removed and slides back, short reverse paper swipe', 1.0)],
    [c('resolve_start', 'A queued sequence of fantasy card actions starts resolving, compact energetic mechanical release', 1.5)],
    [c('cards_discard', 'Played fantasy cards slide together into a discard pile, brief layered paper rustle', 1.2)],
    [c('player_turn_end', 'Player ends the turn and discards the hand, soft card sweep ending with a low closure tap', 1.5)],
    [c('enemy_turn_start', 'Enemy turn begins, subtle ominous low metallic marker', 1.1), c('enemy_turn_end', 'Enemy turn ends, subtle low metallic marker resolving upward', 1.1)],
  ]},
  { dir: 'Board', rows: [
    [c('tile_swap', 'Two magical stone tiles quickly swap positions on a board, crisp paired sliding clicks', 1.2)],
    [c('tile_rotate', 'A group of magical stone tiles rotates together, several smooth stone slides and a locking tick', 1.5), c('row_shift', 'A full row of magical stone tiles shifts across the board, layered stone movement and a stop', 1.6)],
    [c('tile_recolor', 'A magical tile changes color, short sparkling transmutation sweep with a clean finish', 1.2)],
    [c('tile_crack_mark', 'A stone tile is marked to break, tiny sharp crack without a full shatter', 1.0)],
    [c('tile_break', 'Several magical stone tiles break together in a match three game, crisp crystalline stone shatter, compact and punchy', 1.5)],
    [c('tile_purge', 'A cursed or locked tile is purged, brittle seal breaks and dark magic dissipates', 1.5)],
    [c('gravity_shift', 'The direction of gravity changes on a fantasy tile board, short low spatial rotation whoosh ending in a tick', 1.6)],
    [c('board_shuffle', 'Many magical stone tiles shuffle rapidly across a board, layered rolling clicks ending in a firm settle', 1.9)],
    [c('tile_fall', 'A few small stone tiles fall into empty places and settle, light irregular clicks', 1.2)],
    [c('tile_spawn', 'New magical tiles appear on a board, delicate brief crystalline pops', 1.1)],
    [c('match_wave_complete', 'One wave of matching tiles finishes, concise bright magical completion accent', 1.2)],
    [c('big_match', 'A large match of magical tiles completes, stronger sparkling and resonant reward accent', 1.5)],
    [c('cascade_wave', 'Another wave of a match three cascade begins, rising crystalline chime that supports repeated layering', 1.3), c('super_match', 'A super match reaches its finale, triumphant compact burst of crystalline magic and deep impact', 1.8)],
    [c('mana_gain', 'Mana increases after a tile match, short liquid magical energy filling a small vessel', 1.2)],
    [c('shard_collect', 'Glowing shards appear and fly into a counter, several tiny glass sparkles ending in a soft collection ping', 1.5)],
    [c('tile_bonus_mana', 'A tile grants bonus mana, a tiny blue magical fill and bright ping', 1.1), c('tile_bonus_heal', 'A tile grants health, gentle warm restorative pulse', 1.1), c('tile_bonus_action', 'A tile grants an extra action point, brisk energetic ticking chime', 1.1), c('tile_bonus_shield', 'A tile grants armor, small metallic shield glint', 1.1), c('tile_bonus_card', 'A tile grants an extra card, light paper flick and magical ping', 1.1)],
    [c('tile_bonus_ignite', 'A tile ignites an enemy, quick crackling magical flame flare', 1.2), c('tile_bonus_neighbor_break', 'A tile breaks its neighboring stone tile, brief sharp two-stage crack', 1.2)],
  ]},
  { dir: 'Combat', rows: [
    [c('enemy_intent', 'An enemy reveals its planned action, tense warning sting of low metal and faint magic', 1.3)],
    [c('player_attack_windup', 'A fighter winds up a basic sword attack, short weighted blade movement through air', 1.2)],
    [c('player_projectile_launch', 'A basic magical attack projectile launches, fast focused burst and passing whoosh', 1.2)],
    [c('enemy_projectile_hit', 'A magical projectile strikes an enemy, crisp impact with a brief arcane crackle', 1.2)],
    [c('player_hit', 'An enemy skill hits the player, solid dark fantasy impact with a short body and armor reaction', 1.2)],
    [c('shield_hit', 'An attack strikes a sturdy protective shield, hard metal and magical barrier impact', 1.2)],
    [c('shield_appear', 'A protective magic shield appears around a fighter, rising resonant metallic shimmer', 1.3), c('shield_fade', 'A protective magic shield disappears, soft descending metallic shimmer', 1.2)],
    [c('enemy_death', 'A defeated fantasy enemy collapses, a compact dark impact and fading magical residue', 1.8), c('player_death', 'The player falls in a dark fantasy battle, heavy armor collapse and somber fading tone', 2.0)],
    [c('player_skill_cast', 'A player casts a fantasy battle skill, clean assertive arcane release', 1.3), c('enemy_skill_cast', 'An enemy casts a fantasy battle skill, rough ominous arcane release', 1.3)],
    [c('multi_hit_tick', 'One quick additional hit in a multi strike skill, short sharp blade and armor impact for repeated use', 1.0)],
    [c('healing', 'A fantasy character is healed, warm soft magical swell resolving in a bright restorative ping', 1.5), c('regeneration_tick', 'A small regeneration pulse heals at turn start, gentle brief magical heartbeat', 1.1)],
    [c('shield_apply', 'A defensive effect is applied, a sturdy magical shield snaps into place with a metallic gleam', 1.4)],
    [c('status_apply', 'A magical status is applied to a fighter, compact arcane mark sound', 1.1), c('status_strengthen', 'An existing magical status grows stronger, rising layered arcane pulse', 1.3), c('status_expire', 'A magical status expires, small fading arcane release', 1.1)],
    [c('burn_tick', 'A burning status deals damage at the start of a turn, quick hot flame crackle and pain impact, no voice', 1.2)],
    [c('mana_transmute', 'Health or energy is transmuted into mana, swirling alchemical magical exchange', 1.5), c('hp_sacrifice', 'A fighter sacrifices health for power, dark sharp ritual pulse', 1.5), c('card_recover', 'A lost fantasy card is recovered, reverse paper sweep with a magical snap', 1.3), c('soul_collect', 'Glowing souls are gathered, several airy spectral wisps converge into one resonant pulse', 1.7)],
  ]},
  { dir: 'Rewards', rows: [
    [c('victory', 'A dark fantasy battle victory, brief uplifting brasslike and magical cadence with a decisive final hit, not a song', 2.2), c('defeat', 'A dark fantasy battle defeat, short descending somber metallic cadence and final low impact, not a song', 2.2)],
    [c('battle_rewards_reveal', 'Battle rewards and magical shards appear, several soft sparkles and a satisfying treasure accent', 1.6)],
    [c('experience_fill', 'An experience bar fills, a restrained rising magical stream ending in a soft tick', 1.5)],
    [c('level_up', 'A character levels up and gains stat points, uplifting compact magical burst and bright ending chime', 2.0)],
    [c('battle_result_close', 'The battle result panel closes and the map returns, soft parchment fold into an arrival accent', 1.5)],
    [c('boss_reward_reveal', 'A rare boss reward is revealed, deep treasure resonance and magical sparkle', 1.8), c('new_skill_choose', 'A new fantasy skill is selected, strong precise arcane confirmation chime', 1.5)],
    [c('skill_equip', 'A fantasy skill is equipped, firm magical slot-in click', 1.1), c('skill_unequip', 'A fantasy skill is removed from equipment, light reverse magical slot click', 1.1)],
    [c('deck_card_add', 'A fantasy card is added to the deck, one crisp paper slide into a stack and click', 1.2), c('deck_card_remove', 'A fantasy card is removed from the deck, one reverse paper slide', 1.1)],
    [c('card_filter_change', 'A card list filter changes, quiet precise fantasy UI tick', 1.0), c('card_page_change', 'A list of cards turns to the next page, small parchment page flip', 1.1)],
    [c('stat_point_add', 'A stat point is assigned, focused rising tiny chime', 1.0), c('stat_point_undo', 'A stat point assignment is undone, soft descending tick', 1.0), c('stat_points_confirm', 'Stat point changes are confirmed, firm compact triumphant chime', 1.2)],
    [c('card_recipe_select', 'A card crafting recipe is selected, paper and small tool click', 1.1), c('card_craft_success', 'A fantasy card is successfully crafted, paper, subtle magic and a clean rewarding final ping', 1.8)],
    [c('tile_type_select', 'A tile type is selected in a crafting interface, small stone click', 1.0), c('tile_upgrade_select', 'A tile upgrade is selected, tiny runic sparkle and stone tick', 1.0), c('tile_slot_select', 'A tile crafting slot is selected, precise socket click', 1.0), c('tile_upgrade_remove', 'An upgrade is removed from a tile, soft reverse rune release', 1.1)],
    [c('tile_craft_success', 'A magical stone tile is successfully forged, short hammer tap and crystalline completion shimmer', 1.8)],
    [c('ui_hover', 'A very subtle fantasy interface hover cue, a single soft dry tick suitable for cards and skill buttons', 0.9)],
  ]},
];

const skillGroups = [
  { name: 'Podstawowe', dir: 'Basic', layer: c('basic_layer', 'A restrained shared layer for basic fantasy skills, a tiny steel and parchment activation accent', 1.1), skills: [
    ['BasicAttackSkill', 'A simple sword attack starts, brisk blade movement and steel edge'],
    ['BasicHeal', 'A basic healing spell blooms, soft warm restorative glimmer'],
    ['GuardUp', 'A guard stance rises, shield braces with a firm steel click'],
    ['Ignite', 'A small flame ignites instantly, tight fire crackle'],
    ['ShieldBash', 'A shield drives forward with a blunt metal strike'],
  ]},
  { name: 'Wrogowie', dir: 'Enemy', layer: c('enemy_layer', 'A restrained shared layer for enemy fantasy skills, rough dark low magical activation', 1.1), skills: [
    ['EnemyEmber', 'An enemy releases a tiny ember projectile, harsh fire spit'],
    ['EnemyGuard', 'An enemy raises its guard, rough armor and shield scrape'],
    ['EnemySlam', 'An enemy slams into the ground, heavy blunt impact'],
    ['EnemyStrike', 'An enemy makes a quick rough weapon strike, gritty steel swish'],
  ]},
  { name: 'Mag', dir: 'Mage', layer: c('mage_layer', 'A restrained shared layer for mage skill casts, arcane energy gathers and releases softly', 1.1), skills: [
    ['ChromaticBolt', 'A prismatic magical bolt shoots out with a bright chromatic spark'],
    ['Convergence', 'Several magical threads converge into one focused pulse'],
    ['FireSpark', 'A tiny fire spark leaps and crackles sharply'],
    ['IceWard', 'A protective shell of ice rapidly crystallizes'],
    ['IgniteBurst', 'A compact burst of magical fire flashes outward'],
    ['Kindle', 'A small existing flame grows brighter with a short flare'],
    ['LightningChain', 'An electrical arc jumps between several targets in quick sequence'],
    ['Renew', 'Warm restorative magic pulses with a soft luminous shimmer'],
    ['TransmuteMana', 'Arcane energy changes form in a short alchemical swirl'],
    ['Wildfire', 'A sweeping wave of magical fire rushes outward and fades'],
  ]},
  { name: 'Wojownik', dir: 'Warrior', layer: c('warrior_layer', 'A restrained shared layer for warrior skills, strong leather movement and steel readiness', 1.1), skills: [
    ['BattleFrenzy', 'A warrior enters battle frenzy, tense rapid steel rattles and forceful breathlike rush, no voice'],
    ['Execute', 'A decisive finishing blade chop lands with great weight'],
    ['Fortress', 'A massive armored defense locks into place, thick shield and stone resonance'],
    ['GuardStance', 'A fighter plants their stance and firmly raises a shield'],
    ['HeavySlash', 'A heavy sword cuts through air in a broad forceful swing'],
    ['PerfectStrike', 'A perfectly timed fast blade strike makes a precise piercing impact'],
    ['Riposte', 'A quick parry instantly turns into a sharp counterattack'],
    ['ShieldSlam', 'A solid shield slams into an opponent with metallic weight'],
    ['Unyielding', 'An armored fighter withstands a blow, resilient steel rings and settles'],
    ['WideSwing', 'A broad sword sweeps across multiple enemies in one motion'],
  ]},
  { name: 'Cień', dir: 'Shadow', layer: c('shadow_layer', 'A restrained shared layer for shadow skills, soft dark airy movement and a muted blade glint', 1.1), skills: [
    ['Backstab', 'A concealed dagger makes one sudden precise stab from behind'],
    ['BladeDance', 'Several light blades flash in a rapid graceful flurry'],
    ['BloodPact', 'A dark blood pact seals with a low ritual heartbeat and spectral snap'],
    ['ExposeWeakness', 'A hidden armor weakness is revealed, thin metallic fracture and dark glint'],
    ['KillingSequence', 'A rapid escalating sequence of precise dagger cuts'],
    ['Momentum', 'A shadow fighter accelerates forward with a swift airy rush'],
    ['QuickCut', 'One extremely fast, sharp dagger slice'],
    ['Recycle', 'A discarded card returns through shadow magic with reverse paper movement'],
    ['ShadowHarvest', 'Dark spectral energy is drawn inward from a defeated foe'],
    ['SoulBlade', 'An ethereal soul-infused blade cuts with a ghostly metallic resonance'],
  ]},
];

const snake = value => value.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase();
const events = eventGroups.flatMap(group => group.rows.map(cues => ({ dir: group.dir, cues })));
if (events.length !== 76) throw new Error(`Oczekiwano 76 punktów, jest ${events.length}`);
const skillCues = skillGroups.flatMap(group => [
  { dir: join('Skills', group.dir), cue: group.layer },
  ...group.skills.map(([name, description]) => ({ dir: join('Skills', group.dir), cue: c(snake(name), description, 1.3) })),
]);
const allCues = [
  ...events.flatMap(event => event.cues.map(cue => ({ dir: event.dir, cue }))),
  ...skillCues,
];
const pathFor = ({ dir, cue }) => join(audioRoot, dir, `${cue.name}.mp3`);
const relativePathFor = ({ dir, cue }) => `Assets/_Project/Audio/SFX/${dir.replaceAll('\\', '/')}/${cue.name}.mp3`;
const present = item => existsSync(pathFor(item)) && statSync(pathFor(item)).size > 1000;
const sleep = ms => new Promise(r => setTimeout(r, ms));
const basePrompt = 'Dark fantasy turn-based match-three game sound effect. Single isolated cue. No background ambience, no music, no voices. Clean attack and quick decay. ';

function updateDocument() {
  let doc = readFileSync(docPath, 'utf8');
  doc = doc.replace('W `Assets/_Project` nie ma obecnie plików audio ani odtwarzania SFX w skryptach gry.',
    'W `Assets/_Project/Audio/SFX` znajdują się wygenerowane próbki. Ścieżki poniżej wskazują pliki audio; samo wyzwalanie SFX w skryptach gry pozostaje do wdrożenia.');
  const lines = doc.split(/\r?\n/);
  let eventIndex = 0;
  const updated = lines.map(line => {
    if (!line.startsWith('- [ ]')) return line;
    const event = events[eventIndex++];
    if (!event) throw new Error('Dokument ma więcej punktów niż manifest');
    const clean = line.replace(/ \*\*SFX:\*\*.*$/, '');
    const links = event.cues.map(cue => ({ dir: event.dir, cue })).filter(present)
      .map(item => `\`${relativePathFor(item)}\``);
    return links.length ? `${clean} **SFX:** ${links.join(', ')}` : clean;
  });
  if (eventIndex !== events.length) throw new Error(`Dokument ma ${eventIndex} punktów zamiast ${events.length}`);
  doc = updated.join('\n');
  const startMarker = '<!-- GENERATED-SKILL-SFX-START -->';
  const endMarker = '<!-- GENERATED-SKILL-SFX-END -->';
  const table = [startMarker, '| Grupa | Umiejętność / warstwa | SFX |', '| --- | --- | --- |'];
  for (const group of skillGroups) {
    const layerItem = { dir: join('Skills', group.dir), cue: group.layer };
    table.push(`| ${group.name} | Wspólna warstwa | ${present(layerItem) ? `\`${relativePathFor(layerItem)}\`` : '—'} |`);
    for (const [name, description] of group.skills) {
      const item = { dir: join('Skills', group.dir), cue: c(snake(name), description) };
      table.push(`| ${group.name} | ${name} | ${present(item) ? `\`${relativePathFor(item)}\`` : '—'} |`);
    }
  }
  table.push(endMarker);
  if (doc.includes(startMarker)) {
    doc = doc.replace(new RegExp(`${startMarker}[\\s\\S]*?${endMarker}`), table.join('\n'));
  } else {
    doc = doc.replace(/^- \*\*Podstawowe:\*\*.*\n^- \*\*Wrogowie:\*\*.*\n^- \*\*Mag:\*\*.*\n^- \*\*Wojownik:\*\*.*\n^- \*\*Cień:\*\*.*$/m, table.join('\n'));
  }
  writeFileSync(docPath, doc, 'utf8');
}

let done = 0;
let failed = 0;
const failures = [];
console.log(`Plan: ${events.length} punktów, ${allCues.length} plików SFX (warianty i umiejętności).`);
for (const [index, item] of allCues.entries()) {
  const output = pathFor(item);
  mkdirSync(dirname(output), { recursive: true });
  if (!present(item) && item.cue.seed) {
    const source = join(audioRoot, item.cue.seed);
    if (existsSync(source)) copyFileSync(source, output);
  }
  if (present(item)) {
    done++;
    console.log(`[${index + 1}/${allCues.length}] OK ${relativePathFor(item)} (istnieje)`);
    continue;
  }
  let error = '';
  for (let attempt = 1; attempt <= 3; attempt++) {
    const args = [cliPath, 'text-to-sound-effects', 'convert', '--text', basePrompt + item.cue.description,
      '--duration-seconds', String(item.cue.seconds), '--output-format', 'mp3_44100_128', '--output', output];
    const result = spawnSync(process.execPath, args, { cwd: toolDir, env: { ...process.env, ELEVENLABS_API_KEY: apiKey },
      encoding: 'utf8', timeout: 120_000, maxBuffer: 1024 * 1024 });
    if (result.status === 0 && present(item)) { error = ''; break; }
    error = `${result.error?.message ?? ''} ${result.stderr ?? ''} ${result.stdout ?? ''}`.replaceAll(apiKey, '[REDACTED]');
    if (!/429|500|502|503|504|networkError|timed out/i.test(error) || attempt === 3) break;
    await sleep(1500 * attempt);
  }
  if (present(item)) {
    done++;
    console.log(`[${index + 1}/${allCues.length}] OK ${relativePathFor(item)}`);
  } else {
    failed++;
    failures.push(`${relativePathFor(item)}: ${error.slice(0, 400)}`);
    console.log(`[${index + 1}/${allCues.length}] BŁĄD ${relativePathFor(item)}`);
    if (/401|402|insufficient|quota|credits/i.test(error)) {
      console.log('Generowanie przerwane: uwierzytelnienie lub limit konta.');
      break;
    }
  }
  await sleep(200);
}
updateDocument();
console.log(`WYNIK: ${done}/${allCues.length} plików, błędy: ${failed}`);
for (const failure of failures) console.log(failure);
if (done !== allCues.length) process.exitCode = 1;
