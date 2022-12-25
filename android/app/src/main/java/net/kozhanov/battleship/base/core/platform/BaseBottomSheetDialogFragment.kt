package net.kozhanov.battleship.base.core.platform

import android.app.Dialog
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.annotation.LayoutRes
import com.google.android.material.bottomsheet.BottomSheetBehavior
import com.google.android.material.bottomsheet.BottomSheetDialog
import com.google.android.material.bottomsheet.BottomSheetDialogFragment
import net.kozhanov.battleship.base.extensions.launchAndCollectIn

abstract class BaseBottomSheetDialogFragment<VIEW_STATE>(
    @LayoutRes val contentLayoutId: Int,
    private val isNeedFixBehavior: Boolean = true
) : BottomSheetDialogFragment() {

    abstract val viewModel: BaseViewModel<VIEW_STATE>

    abstract fun setupUI()

    abstract fun render(viewState: VIEW_STATE)

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? = LayoutInflater.from(requireContext()).inflate(contentLayoutId, null)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        setupUI()
        setupObserve()
    }

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val dialog = super.onCreateDialog(savedInstanceState) as BottomSheetDialog
        if (isNeedFixBehavior) {
            dialog.fixBottomSheetBehavior()
        }
        return dialog
    }

    open fun setupObserve() {
        viewModel.viewState.launchAndCollectIn(viewLifecycleOwner) { viewState ->
            render(viewState)
        }

        viewModel.singleEvent.launchAndCollectIn(viewLifecycleOwner) { event ->
            singleEvent(event)
        }
    }

    open fun singleEvent(state: SingleEvent) {
        // nothing
    }

    /**
     * Убирает промежуточный вариант повдеения для ботомшитов, где это не требуется
     */
    private fun BottomSheetDialog.fixBottomSheetBehavior() {
        behavior.state = BottomSheetBehavior.STATE_EXPANDED
        behavior.skipCollapsed = true
        behavior.addBottomSheetCallback(object : BottomSheetBehavior.BottomSheetCallback() {

            override fun onStateChanged(bottomSheet: View, newState: Int) {
                if (newState == BottomSheetBehavior.STATE_COLLAPSED) dismiss()
            }

            override fun onSlide(bottomSheet: View, slideOffset: Float) = Unit
        })
    }
}
