using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class Insults
    {
        private static int _lastInsultIndex = -1;
        public static string GetRandomInsult()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, InsultList.Count);
            while(randomNumber == _lastInsultIndex)
            {
                randomNumber = random.Next(0, InsultList.Count);
            }

            return InsultList[randomNumber];
        }

        private static List<string> InsultList = new List<string>()
        {
            "you are the human version of a headache.",
            "if you were a vegetable you'd be a cabbitch.",
            "I don't exactly hate you but if you were on fire and I had water I'd drink it.",
            "your birth certificate is an apology letter from the condom factory.",
            "you remind me of a penny, two-faced and not worth much.",
            "if your parents were to divorce, would they still be brother and sister?",
            "I treasure the time I don't spend with you.",
            "you are so fat you fall off both sides of the bed.",
            "calling you an idiot would be an insult to all the stupid people.",
            "you are about as useful as a knitted condom.",
            "you are so ugly you make blind kids cry.",
            "I would make a joke about your life but I see life already beat me to it.",
            "your Mother should've swallowed you.",
            "you make me wish I had more middle fingers.",
            "you'll never be the man that your Mother is.",
            "I'm not saying I hate you but I would unplug your life support to charge my phone.",
            "the last time I saw something like you.. I flushed it.",
            "I guess those penis enlargement pills are working, you're twice the dick you were yesterday.",
            "shit happens. I mean, look at your face.",
            "everyone's entitled to be stupid but you're abusing the privilege.",
            "I've met some pricks in my time, but you my friend, are the f*cking cactus.",
            "is your ass away that your head has moved in?",
            "you look like something I drew with my left hand.",
            "if I had a monkey with a face like yours, I'd shave his arse and teach him to walk backwards.",
            "you're the reason they have to put directions on shampoo.",
            "why don't you slip into something more comfortable, like a coma?!",
            "I'd slap you but that would be animal abuse!",
            "you have a great face for make-up!",
            "I'm typing this with my middle finger.",
            "I'm busy. You're ugly. Have a nice day!",
            "you are a bag of douche!",
            "I have neither the time nor the crayons to explain how stupid you are to you.",
            "you bring everyone a lot of joy when you leave the room!",
            "your family tree must be a cactus because everyone on it is a prick!",
            "your face makes onions cry.",
            "you fear success, but really have nothing to worry about!",
            "if I gave you a penny for your thoughts, I'd get change!",
            "if you spoke your mind you'd be speechless!",
            "looks like you fell off the ugly tree and hit every branch on the way down.",
            "you're so fat you could sell shade.",
            "you are proof that God has a sense of humour.",
            "did your parents ever ask you to run away from home?",
            "100,000 sperm, you were the fastest?",
            "yo mama's so fat, when she goes camping, the bears hide *their* food.",
            "yo mama's so fat, if she buys a fur coat, a whole species will become extinct.",
            "yo mama's so fat, when she wears high heels, she strikes oil.",
            "yo mama's so fat, if she was a Star Wars character, her name would be Admiral Snackbar.",
            "yo mama's so fat, she brought a spoon to the Super Bowl.",
            "yo mama's so stupid, she stared at a cup of orange juice for 12 hours because it said \"concentrate.\"",
            "yo mama's so stupid, she got hit by a parked car.",
            "yo mama's so ugly, she threw a boomerang and it refused to come back.",
            "yo mama's so ugly, she made a blind kid cry.",
            "yo mama's so old, she walked out of a museum and the alarm went off.",
            "yo mama's so ugly, she looked out the window and was arrested for mooning.",
            "yo mama's so poor, the ducks throw bread at her.",
            "yo mama so old, I told her to act her own age, and she died.",
            "yo mama so scary, the government moved Halloween to her birthday.",
            "yo mama's so nasty, they used to call them jumpolines 'til yo mama bounced on one.",
            "yo mama's so poor, Nigerian princes wire her money.",
            "I wanted to be you for Halloween but I couldn't fit seven dicks in my mouth.",
            "you should change your name to \"Whore-a the Explorer\".",
            "two wrongs don't make a right, take your parents for example.",
            "yo mama's so fat, not even Dora can explore her",
            "I could get on your level, but I don't like being on my knees as much as you do.",
            "I could tell you to eat shit, but that would be cannibalism",
            "what's the difference between you and eggs? Eggs get laid.",
            "why play so hard to get when you're already so hard to want.",
            "you talk so much shit I don't know whether to offer you a breath mint or toilet paper",
            "you're like a windows update. Whenever I see you I think, not now",
            "you must have been born on a motorway because that's where most accidents happen",
            "if you were anymore inbred, you would be a sandwich",
            "don't be ashamed of who you are. That's your parent's job!",
            "mirrors don't lie and lucky for you they don't laugh.",
            "you're like the top piece of bread. Everyone touches you, but nobody wants you.",
            "some people are such treasures that you just want to bury them.",
            "I'm no cactus expert but I know a prick when I see one.",
            "what doesn't kill you..... disappoints me.",
            "if zombies chase us, I'm tripping you. Nothing personal.",
            "I'm trying to see things from your point of view but I can't stick my head that far up my ass.",
            "in the wise words of Master Yoda - \"Stupid you are, breed you should not\".",
            "if you were a cookie, you'd be a whoreo.",
            "being a dick won't make yours any bigger.",
            "yo mama's so old she was a waitress at the last supper.",
            "don't you need a license to be that ugly?",
            "in the battle of wits, you fight unarmed",
            "I heard you went to a haunted house and they offered you a job.",
            "you have the face of a saint. Saint Bernard, that is.",
            "the twinkle in your eyes is actually the sun shining between your ears.",
            "Reverse uno card. Stop trying to insult people, {userId} you are ugly and nobody likes you!"
        };
    }
}
