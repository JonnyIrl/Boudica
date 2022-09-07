using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class Compliments
    {
        private static int _lastIndex = -1;
        public static string GetRandomCompliment()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, ComplimentList.Count);
            while (randomNumber == _lastIndex)
            {
                randomNumber = random.Next(0, ComplimentList.Count);
            }

            return ComplimentList[randomNumber];
        }

        private static List<string> ComplimentList = new List<string>()
        {
            "I would still hang out with you even if you haven’t showered for days.",
            "your face makes other people look ugly.",
            "you know, you’re almost as wonderful as cake. Almost.",
            "let’s say you were cloned. I bet you’d still be one of a kind. And the better looking one!",
            "are you a beaver, because damn!",
            "truth be told, you have really good taste in friends (i.e. me).",
            "aside from food, you are my favorite.",
            "it certainly is not easy to be me, which is why I need you.",
            "If it was legal to marry food, I’d still choose you over pizza.",
            "you’re more fun than bubble wrap.",
            "in a world full of bagels, you’re a doughnut.",
            "if you were a dog, you’d either be the leader of the pack or the laziest one in the world. Sometimes, I just can’t tell with you.",
            "you’re someone that I don’t want to punch in the throat.",
            "in school, I bet you were voted “most likely to keep being awesome.”",
            "you’re so beautiful I would definitely steal your photos, make a fake account, and impress people online... brb",
            "you’re such a darling that if I suddenly turned into a psycho-maniac murderer, I’d kill you last.",
            "you know what? I just don't know what’s it about you! You're so irritating yet likable at the same time!",
            "I honestly think you can do anything you pour your mind into. But, I also know what kind of thoughts you have, so maybe hold off on that for now?",
            "you have a unique set of skills that can somehow turn any situation into an awkward one.",
            "you are definitely not someone who I pretend not to see in public.",
            "if you were a box of crayons, you’d be the gigantic branded variety with the built-in sharpener... unlike those dirty Titans",
            "you are like mathematics. You're difficult at times, but worth getting to know.",
            "you are perfectly imperfect. And that’s just perfect.",
            "I don’t think you’re clumsy. The floor and the walls are just really friendly to you.",
            "if there’s one thing that I like about you, it’s that I like more than just one thing about you.",
            "on a scale of 1 to 10, you’re an 11.",
            "our time together is like a nap. It just doesn’t last long enough... that's what she said!",
            "you were cool way before hipsters were cool.",
            "we all have those days where it’s like, “Yeah, I’m not getting anything done today.” And on those days, I know I can trust you to join me in accomplishing nothing.",
            "your humour is like a dog whistle. It mostly goes undetected. But to those that get it, they really get it.",
            "I’m so luckily that you’re not a drug. If you were, I would turn into an unreasonable addict, and then I’d have to go for rehab..",
            "you're at the top of the bell curve!",
            "you're like that one sock that disappears out of the blue. I don't know what I did to lose you, but I want you back.",
            "the people who raised you deserve a medal for a job well done.",
            "you’re the human embodiment of the fanny pack. You’re cool, but in your own way.",
            "you could never be ice cream, because you’re so hot.",
            "you’re so damn sexy that people under 18 shouldn’t be allowed to look at you without parental supervision.",
            "damn, you’re hot. You must be the reason for global warming.",
            "I don’t really have a favorite color. It’s always pretty much the color you are wearing for the day.",
            "if you knew how much I think about you, I would be very embarrassed.",
            "you make me feel feelings that I’m not really sure how to deal with. But, I sort of like it.",
            "you know what’s awesome? Chocolate cake! And oh, your face as well.",
            "I can never remember my dreams, but I assume you are always in them.",
            "you may not be ridiculously good-looking, but you’re pretty damn close. As in super close!",
            "I know you’ve what it takes to survive a zombie apocalypse.",
            "babies and small animals probably like you.",
            "puppies and kittens should fear your cuteness!",
            "is there anything you can’t do?",
            "no one is quite like you. You’re one of a kind!",
            "you significantly bring up the average of human goodness.",
            "the only thing better than being friends with you, is being friends with a talking dolphin.",
            "you are cooler than secret handshake.",
            "I bet you’re smarter than Google.",
            "I really like that you understand my sarcasm because it’s in an advanced form and not everyone gets it.",
            "I don’t really like people, but you’re an exception.",
            "I don’t know if sarcasm is a skill, but you’ve certainly mastered it.",
            "actions speak louder than words, and yours tell an incredible story.",
            "you are quite adept at seeing the best in people, even when everyone else sees the worst.",
            "you’re so fun and cute, I bet you sweat glitter.",
            "if you were a vegetable, you’d be a cute-cumber.",
            "you should be proud of yourself.",
            "you’re so cute that puppies and kittens send pictures of you to each other.",
            "how do you keep being so funny and making everyone laugh?",
            "I look at you the same way we all look at giraffes, which is basically like, “I bet you were just born awesome.”"
        };
    }
}
