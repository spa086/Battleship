package ru.openbank.accept.base.extensions

import androidx.constraintlayout.motion.widget.MotionLayout

fun MotionLayout.onTransitionCompleted(action: () -> Unit) {
    setTransitionListener(object : MotionLayout.TransitionListener {
        override fun onTransitionStarted(motionLayout: MotionLayout, startId: Int, endId: Int) {
            // nothing
        }

        override fun onTransitionChange(motionLayout: MotionLayout, startId: Int, endId: Int, progress: Float) {
            // nothing
        }

        override fun onTransitionCompleted(motionLayout: MotionLayout, currentId: Int) {
            action()
        }

        override fun onTransitionTrigger(
            motionLayout: MotionLayout,
            triggerId: Int,
            positive: Boolean,
            progress: Float
        ) {
            // nothing
        }
    })
}
