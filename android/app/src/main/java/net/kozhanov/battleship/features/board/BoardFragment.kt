package net.kozhanov.battleship.features.board

import androidx.core.view.isVisible
import by.kirich1409.viewbindingdelegate.viewBinding
import net.kozhanov.battleship.R
import net.kozhanov.battleship.base.core.data.models.Ship
import net.kozhanov.battleship.base.core.platform.BaseFragment
import net.kozhanov.battleship.base.core.platform.SingleEvent
import net.kozhanov.battleship.base.extensions.setThrottledClickListener
import net.kozhanov.battleship.databinding.FragmentBoardBinding
import net.kozhanov.battleship.features.board.BoardUIEvent.OnBoardTap
import net.kozhanov.battleship.features.board.BoardUIEvent.StartGame
import net.kozhanov.battleship.features.board.BoardViewState.State.*
import org.koin.androidx.viewmodel.ext.android.viewModel
import timber.log.Timber

class BoardFragment : BaseFragment<BoardViewState>(R.layout.fragment_board) {
    private val binding: FragmentBoardBinding by viewBinding(FragmentBoardBinding::bind)
    override val viewModel: BoardViewModel by viewModel()

    override fun setupUI() {
        binding.start.setThrottledClickListener {
            viewModel.processUiEvent(StartGame)
        }
        binding.board.setOnCellClickListener { x, y ->
            viewModel.processUiEvent(OnBoardTap(x, y))
        }
    }

    override fun render(viewState: BoardViewState) {
        with(binding) {
            Timber.d("state is ${viewState.state}")
            when (viewState.state) {
                Loading -> {}
                Init -> {}
                is Board -> TODO()
                is CreatingShip -> {
                    Timber.d("create fleet")
                    binding.board.setShips(listOf(Ship(viewState.state.decks)))
                }
                is Message -> {
                    title.text = viewState.state.title
                    subtitle.text = viewState.state.text
                }
            }
            binding.progressBar.isVisible = viewState.isLoadingVisible
            binding.title.isVisible = viewState.isTitleVisible
            binding.subtitle.isVisible = viewState.isSubTitleVisible
            binding.board.isVisible = viewState.isBoardVisible
            binding.nextShip.isVisible = viewState.isNextShipVisible
            binding.start.isVisible = viewState.isStartVisible
        }
    }

    override fun singleEvent(event: SingleEvent) {

    }
}