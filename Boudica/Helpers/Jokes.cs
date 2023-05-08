using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class Jokes
    {
        private static int _lastJokeIndex = -1;
        public static string GetRandomJoke()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, JokesList.Count);
            while (randomNumber == _lastJokeIndex)
            {
                randomNumber = random.Next(0, JokesList.Count);
            }

            return JokesList[randomNumber];
        }

        private static List<string> JokesList = new List<string>()
        {
            "The economy in the UK is getting so dire that the elderly aren't getting to enjoy their retirement.\n\nThe BBC interviewed 73 year old Charles from Windsor: \"despite having a generous government pension, I've had to start working today.\"",
            "Why are the pyramids located in Egypt?\n\nThey were too big to transport to England",
            "What's the Longest Word in English?\n\nSmiles\n\nBecause both the first and the last letters are a mile apart",
            "How many Trump supporters does it take to change a lightbulb?\n\nNone. Trump says it’s changed and his supporters all cheer in the dark.",
            "I taught my kids about democracy tonight by having them vote on what movie to watch and pizza to order\n\nAnd then I picked the movie and pizza I wanted because I'm the one with the money.",
            "If you take the first two letters of the title of each the 7 Harry Potter books, it spells out a secret message\n\nHAHAHAHAHAHAHA",
            //"Having homosexual parents must be terrible\n\nEither you have double dosage of dad jokes or you are stuck in cycle of “go ask your mom”",
            "I am getting so sick of millennials and their attitude.\n\nAlways walkin around like they rent the place.",
            "Did you hear that the US bobsled team put Donald Trump's picture on the front of the sled?\n\nApparently nobody else can make America go downhill faster.",
            "I was in a job interview today when the manager handed me his laptop and said, \"I want you to try and sell this to me.\"\n\nSo I put it under my arm, walked out of the building and went home.\n\nEventually, he called me on my phone and said, \"Bring it back here right now!\"\n\nI replied, \"£100 and it's yours.\"",
            //"A domestic abuser, a klansmen, and a murderer walk into a bar.\n\nBartender: what will it be, officer?",
            "Millennial old folks homes are gonna be awesome!\n\nLAN parties, DnD nights, wheelchair races, having awesome songs from the 2000's as our golden oldies! It'll be great, especially if we can line up our work schedules!",
            "Devil: This is the lake of lava you will be spending eternity in\n\nMe: Actually, since we're underground, it would be magma\n\nDevil: You understand this is why you're here, right?",
            "I came home really drunk last night and my wife wasn’t happy at all. “How much have you had to drink?” she asked sternly, staring at me. “Nothing” I slurred. “Look at me!” she shouted. “It’s either me or the pub, which one is it?” I paused for a second while I thought and mumbled...\n\n“It’s you. I can tell by the voice.”",
            "Cashier: that’ll be £19.99\n\nMe: *pulls out a £50*\n\nCashier: sorry we’ve been having problems with counterfeit money… Have anything smaller?\n\nMe: Sure! *pulls out a £30*",
            "What is a Karen called in Europe?\n\nAn American.",
            "Breaking News Trump’s personal library just burned down\n\nThe fire consumed both books and he hasn’t even finished coloring the second one",
            "I only believe 12.5% of the Bible\n\nI guess that makes me an eighth-theist",
            "Fun fact: \"sugar\" is the only word in the English language where \"su-\" makes a \"sh\" sound...\n\nAt least, I'm pretty sure that's correct.",
            "Can someone please tell me what the lowest rank in the military is?\n\nEvery time I ask someone they say “it’s private.”",
            "A police man came up to me with a sniffer dog and said, \"This dog tells me you're on drugs.....\"\n\nI said \"I'm on drugs? you're the one talking to dogs.\"",
            "Everyone knows Alan Turing, who cracked Enigma codes.\n\nBut nobody knows his sister Kate, who provided drinks, snacks and sandwiches for him and his colleagues during that time.",
            "My girlfriend borrowed £100 from me. After 3 years, when we separated, she returned exactly £100.\n\nI lost Interest in that relationship.",
            "Andrew Garfield, Tobey McGuire and Tom Holland got into an accident upon arriving at a party.\n\nAs it turns out, they're terrible parallel parkers.",
            "My wife complains to me about constantly being sexually harassed at work​\n\nI told her she can stop working from home and go back to the office",
            "How many Apple engineers does it take to change a lightbulb?\n\nNone. They no longer make that socket, you just buy a new house.",
            "I think my family is racist\n\nI brought my Asian girlfriend home for dinner and my wife and kids were very rude to her.",
            "My attractive female neighbour is completely paranoid. She thinks I'm following or even stalking her\n\nShe is worried that I may be obsessed with her and any time she hears a noise in her house she is...purified? Oh, wait: petrified. Sorry, it's not easy reading a diary through binoculars from a tree.",
            "I've spent an hour and a half now trying to explain \"sunk cost fallacy\" to my son\n\nHe's no nearer understanding it than when we started, and it's giving me a serious headache.\n\nBut if I quit now I'll have had all this for nothing!",
            "A vegan said to me that people who sell meat are disgusting.\n\nI said people who sell fruit and vegetables are grocer.",
            "How many feminists does it take to screw in a light bulb?\n\nThat's not funny.",
            "Girlfriend messaged me: \"helpmyspacebarbrokecanyoucomeoverandgivemeanalternative\"\n\nWhat does 'ternative' mean?",
            "If you ever feel like your job has no purpose, always remember\n\nright now, there is someone who is installing an indicator in a BMW",
            "Because Nintendo's beloved character is Japanese, Mario is his LAST name. His first name?\n\nItsume.",
            "My boyfriend is upset that I have no sense of direction.\n\nSo I packed up my stuff and right.",
            "A flashbang would be completely ineffective against Helen Keller.\n\nBecause she's dead.",
            "I saw my wife walk past me with her sexiest underwear on, which can only mean one thing.\n\nToday is laundry day.",
            "I've been clean for 47 days now.\n\nIt's weird showering everyday but at least I have the heroin to get through it.",
            //"The wage gap isn't real.\n\nMen simply focus on getting the higher paying jobs like scientist, doctor, engineer. Meanwhile, women tend to go towards the lower paying jobs, like female scientist, female doctor and female engineer.",
            "Some jerk glued every card in my deck together so now its just a block of cardboard.\n\nI'm having trouble dealing with it.",
            "I was stuck in quarantine all alone with a deck of cards.\n\nI guess you could say I was in solitaire confinement.",
            //"What's the difference between a cop and a bullet?\n\nWhen a bullet kills someone, you know it's been fired.",
            "How to determine the gender of your cat?\n\nPour some milk in a bowl and place it next to the cat, if she drinks it, your cat is a female, but if he drinks it, the cat is a male",
            "A gorilla visits a pub and orders a pint of beer. 'That'll be £7.00' says the barman\n\nThe gorilla pays and the barman says 'We don't get many gorillas in the pub' the gorilla replies ' I'm not surprised at these prices'",
            //"What's the difference between Putin and Hitler?\n\nHitler knew when to kill himself",
            "I told my daughter, \"Did you know that humans eat more bananas than monkeys?\" She rolled her eyes at me, but I persevered. \"It’s true!\"\n\n\"When was the last time you ate a monkey?!\"",
            "Just found out that the Oscars is a big fucking lie all the way along\n\nThose people they invite to their ceremonies are all paid actors",
            "Amber Heard's net worth is $2.5 million and she now has to pay Johnny Depp $15 million...\n\nYeah, she's forever going to be in Depp!",
            //"Why do trans women go by she/her?\n\nBecause if they went by her/she they'd be chocolate",
            "The CEO of IKEA has just been appointed as the Prime Minister of Sweden.\n\nHe's currently assembling his cabinet.",
            "What do you call a magic dog? A Labracadabrador",
            "I asked my date to meet me at the gym today. She didn't show up. That's when I knew we weren't gonna work out.",
            "Lance is an uncommon name nowadays. But in medieval times people were named Lance a lot.",
            "I stayed up all night wondering where the sun went, then it dawned on me.",
            "I told my wife she was drawing her eyebrows too high. She looked surprised.",
            "I knew a guy who was going to open a pastry shop. But he couldn't raise the dough.",
            "Today a man knocked on my door and asked for a small donation towards the local swimming pool. I gave him a glass of water.",
            "I think my neighbour is stalking me as she's been googling my name on her computer. I saw it through my telescope last night.",
            "Apparently I snore so loudly that it scares everyone in the car I'm driving.",
            "I can't believe I got fired from the calendar factory. All I did was take a day off.",
            "I used to think I was indecisive, but now I'm not too sure.",
            "I started out with nothing, and I still have most of it.",
            "A computer once beat me at chess, but it was no match for me at kick boxing.",
            "I want to die peacefully in my sleep, like my grandfather.. Not screaming and yelling like the passengers in his car.",
            "I just found out I'm colorblind. The diagnosis came completely out of the purple.",
            "I saw an ad for burial plots, and thought to myself this is the last thing I need.",
            "I have a few jokes about unemployed people but it doesn't matter none of them work.",
            "My dad died when we couldn't remember his blood type. As he died, he kept insisting for us to 'be positive,' but it's hard without him.",
            "I was addicted to the hokey pokey... but thankfully, I turned myself around.",
            "My boss is going to fire the employee with the worst posture. I have a hunch, it might be me.",
            "How did I escape Iraq? Iran.",
            "A cop just knocked on my door and told me that my dogs were chasing people on bikes. My dogs don't even own bikes...",
            "I gave up my seat to a blind person in the bus. That is how I lost my job as a bus driver.",
            "R.I.P boiled water. You will be mist.",
            "I got a new pair of gloves today, but they're both 'lefts' which, on the one hand, is great, but on the other, it's just not right.",
            "The guy who invented autocorrect has died...his funfair will be help next sundial.",
            "Why did Han Solo cry during his steak dinner? Because it was Chewie.",
            "My doctor told me I only had six months to live, so I leapt over his desk and stabbed him through the heart with his own pen. Got me twenty years.",
            "I was told, I would never be good at poetry, since I’m dyslexic… But so far I’ve made 3 jugs and a vase… and they look very nice, if you ask me.",
            "So I broke up with my disabled girlfriend and stole her wheelchair.. But guess who came crawling back!!?!",
            "I broke up with my girlfriend because she was a communist. To be honest, there were a lot of red flags",
            "I asked God what the most unlikely thing in the world was. He replied.",
            "When I was young, I was poor. After many years of hard work, I am no longer young.",
            "If smoking marijuana causes short-term memory loss, what does smoking marijuana do?",
            "Why is the British weather like Islam? Because it’s either Sunni or Shi’ite.",
            "It was my birthday yesterday, and I received $500 from all the cards I opened. I really love working in a post office.",
            "What do the movies Titanic and The Sixth Sense have in common? Icy dead people.",
            "What is the difference between Americans and IT support? Americans don't have troubleshooting.",
        };
    }
}
