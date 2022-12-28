package net.kozhanov.battleship.features.board

import androidx.core.view.isVisible
import by.kirich1409.viewbindingdelegate.viewBinding
import net.kozhanov.battleship.R
import net.kozhanov.battleship.base.core.platform.BaseFragment
import net.kozhanov.battleship.base.core.platform.SingleEvent
import net.kozhanov.battleship.base.extensions.setThrottledClickListener
import net.kozhanov.battleship.databinding.FragmentBoardBinding
import net.kozhanov.battleship.features.board.BoardUIEvent.StartGame
import net.kozhanov.battleship.features.board.BoardViewState.State.*
import org.koin.androidx.viewmodel.ext.android.viewModel

class BoardFragment : BaseFragment<BoardViewState>(R.layout.fragment_board) {
    private val binding: FragmentBoardBinding by viewBinding(FragmentBoardBinding::bind)
    override val viewModel: BoardViewModel by viewModel()

    override fun setupUI() {
        binding.start.setThrottledClickListener {
            viewModel.processUiEvent(StartGame)
        }
    }

    override fun render(viewState: BoardViewState) {
        with(binding) {
            when (viewState.state) {
                Loading -> {}
                is Result -> {
                    title.text = viewState.state.text
                    subtitle.text = viewState.state.subtitle
                }
                Init -> {}
            }
            binding.progressBar.isVisible = viewState.isLoadingVisible
            binding.title.isVisible = viewState.isResultVisible
        }
    }

    override fun singleEvent(event: SingleEvent) {

    }
}