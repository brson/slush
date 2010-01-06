using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Slush.Services;
using Slush.Services.Mp3;
using Slush.Validators;
using Slush.Validators.Mp3;

namespace Slush
{
    public class Mp3Validator
    {
        public event ValidationFailureEventHandler OnValidationFailure;

        public void Validate(Stream mp3Stream)
        {
            EventWeaver weaver = new EventWeaver();

            StreamProcessService streamService;
            ValidationFailureCollectorService validationService;
            
            streamService = new StreamProcessService(mp3Stream);
            validationService = new ValidationFailureCollectorService();
            validationService.OnValidationFailure += ValidationFailureEventHandler;

            weaver.Add(streamService);
            weaver.Add(new Mp3CrawlerService());
            weaver.Add(new LameHeaderService());

            weaver.Add(new BrokenFrameValidator());
            weaver.Add(new JunkDataValidator());
            weaver.Add(new LameHeaderPresentValidator());
            weaver.Add(new LameInfoCrcValidator());
            weaver.Add(new LameMusicCrcValidator());
            weaver.Add(new FrameCrcValidator());
            weaver.Add(new AverageBitrateValidator());

            weaver.Add(validationService);

            streamService.Begin();
        }

        public void ValidationFailureEventHandler(ValidationFailureEventArgs e)
        {
            if (OnValidationFailure != null)
            {
                OnValidationFailure(e);
            }
        }
    }
}
