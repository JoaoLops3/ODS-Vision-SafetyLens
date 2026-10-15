#import <UIKit/UIKit.h>

// Declarado pelo runtime Unity (Classes/UI/UnityViewControllerBase).
extern UIViewController* UnityGetGLViewController();

extern "C" bool SafetyLens_IsReduceMotionEnabled()
{
    return UIAccessibilityIsReduceMotionEnabled();
}

extern "C" void SafetyLens_ShareFiles(const char* pathsJoined, const char* subject)
{
    if (pathsJoined == NULL) return;

    NSString *joined = [NSString stringWithUTF8String:pathsJoined];
    NSArray<NSString *> *parts = [joined componentsSeparatedByString:@"|"];
    NSMutableArray *items = [NSMutableArray arrayWithCapacity:parts.count];

    for (NSString *path in parts)
    {
        if (path.length == 0) continue;
        if (![[NSFileManager defaultManager] fileExistsAtPath:path]) continue;
        [items addObject:[NSURL fileURLWithPath:path]];
    }

    if (items.count == 0) return;

    NSString *subj = subject ? [NSString stringWithUTF8String:subject] : @"SafetyLens";

    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *root = UnityGetGLViewController();
        if (root == nil) return;

        UIActivityViewController *activity =
            [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
        [activity setValue:subj forKey:@"subject"];

        // iPad exige âncora de popover; sem isso o presentViewController falha.
        if (activity.popoverPresentationController != nil)
        {
            activity.popoverPresentationController.sourceView = root.view;
            activity.popoverPresentationController.sourceRect =
                CGRectMake(CGRectGetMidX(root.view.bounds), CGRectGetMidY(root.view.bounds), 1.0, 1.0);
            activity.popoverPresentationController.permittedArrowDirections = 0;
        }

        [root presentViewController:activity animated:YES completion:nil];
    });
}
